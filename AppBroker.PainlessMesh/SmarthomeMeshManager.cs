using AppBroker.Core;

using AppBrokerASP.Devices.Painless;

using Fluid.Ast.BinaryExpressions;

using Newtonsoft.Json;

using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AppBroker.PainlessMesh;

public class SmarthomeMeshManager : IDisposable
{
    private class NodeSync
    {
        public uint Id { get; }
        public byte MissedConnections { get; set; }
        public NodeSync(uint id, byte missedConnections)
        {
            Id = id;
            MissedConnections = missedConnections;
        }
    }

    public int ConnectedClients => clients.Count;

    public event EventHandler<BinarySmarthomeMessage>? SingleUpdateMessageReceived;
    public event EventHandler<BinarySmarthomeMessage>? SingleOptionsMessageReceived;
    public event EventHandler<BinarySmarthomeMessage>? SingleGetMessageReceived;
    public event EventHandler<(long id, ByteLengthList)>? NewConnectionEstablished;
    public event EventHandler<uint>? ConnectionLost;
    public event EventHandler<(uint id, ByteLengthList parameter)>? ConnectionReestablished;

    private static readonly ServerSocket ServerSocket = new();

    private List<BaseClient> clients = new List<BaseClient>();
    private readonly TimeSpan waitBeforeWhoIAmSendAgain;
    private readonly List<NodeSync> knownNodeIds;
    private readonly HashSet<long> ownManagedIds = new HashSet<long>();
    private readonly int listenPort;
    private readonly Lazy<PainlessMeshMqttManager> mqttManager =
        new(IInstanceContainer.Instance.GetDynamic<PainlessMeshMqttManager>);
    private readonly ConcurrentDictionary<uint, (DateTime time, int count)> whoIAmSendTime;

    private readonly uint nodeId = 1;
    private readonly Dictionary<uint, Queue<BinarySmarthomeMessage>> queuedMessages;
    private readonly List<Timer> timers;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private bool running;

    public SmarthomeMeshManager(bool enabled, int listenPort, uint nodeId = 1)
    {
        waitBeforeWhoIAmSendAgain = new TimeSpan(0, 0, 30);

        queuedMessages = new();
        timers = new();
        this.nodeId = nodeId;
        whoIAmSendTime = new();
        knownNodeIds = new() { new NodeSync(this.nodeId, 0), new NodeSync(0, 0) };
        this.listenPort = listenPort;
        if (!enabled)
            return;
#if DEBUG
        Task.Delay(5000).ContinueWith(async _ =>
        {
            this.SocketClientDataReceived(null, new BinarySmarthomeMessage(3257232294, MessageType.Update, Command.WhoIAm, Encoding.UTF8.GetBytes("10.9.254.4"), Encoding.UTF8.GetBytes("heater")));
            this.SocketClientDataReceived(null, new BinarySmarthomeMessage(3257171132, MessageType.Update, Command.WhoIAm, Encoding.UTF8.GetBytes("10.9.254.5"), Encoding.UTF8.GetBytes("ledstri")));

        });
#endif
    }

    public void Start(Ota.UpdateManager um)
    {
        if (running)
            return;
        running = true;
        ServerSocket.OnClientConnected += ServerSocket_OnClientConnected;
        ServerSocket.Start(new IPAddress(new byte[] { 0, 0, 0, 0 }), listenPort);

        timers.Add(new Timer(TryCatchWhoAmITask, null, TimeSpan.FromSeconds(15.0), waitBeforeWhoIAmSendAgain));
        timers.Add(new Timer(SendTimeUpdate, null, TimeSpan.FromMinutes(1.0), TimeSpan.FromHours(1d)));
        timers.Add(new Timer(GetMeshUpdate, null, TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10d)));

        um.Advertisment += OtaAdvertisment;
    }

    public void Stop()
    {
        if (!running)
            return;
        running = false;
        ServerSocket.Stop();
        foreach (var item in clients)
        {
            item.Dispose();
        }
        clients.Clear();
        ServerSocket.OnClientConnected -= ServerSocket_OnClientConnected;

        foreach (Timer? timer in timers)
        {
            timer.Dispose();
        }
        timers.Clear();
    }

    private void GetMeshUpdate(object? state)
    {
        if (ConnectedClients > 0)
            SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
    }
    internal void UpdateTime()
    {
        if (ConnectedClients > 0)
            SendTimeUpdate(null);
    }

    private static readonly byte[] fromServer = new byte[] { 1 };
    private void SendTimeUpdate(object? state)
    {
        var dto = new DateTimeOffset(DateTime.Now);
        var list = new ByteLengthList(fromServer, BitConverter.GetBytes((int)dto.ToUnixTimeSeconds()));
        var msg = new BinarySmarthomeMessage(0, MessageType.Update, Command.Time, list);
        SendBroadcast(msg);
    }

    private void OtaAdvertisment(object? sender, PainlessMesh.Ota.FirmwareMetadata e)
    {
        foreach (AppBroker.Core.Devices.Device? item in IInstanceContainer.Instance.DeviceManager.Devices.Values)
        {
            if (item is PainlessDevice pd)
                pd.OtaAdvertisment(e);
        }
    }

    private void ServerSocket_OnClientConnected(object? sender, BaseClient baseClient)
    {
        baseClient.ReceivedData += SocketClientDataReceived;
        clients.Add(baseClient);
        SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
    }
    private void SocketClientDataReceived(object? sender, BinarySmarthomeMessage e)
    {
        SocketClientDataReceived(e);
        mqttManager.Value.EnqueueToMqtt(e);
    }

    public void SocketClientDataReceived(BinarySmarthomeMessage e)
    {
        if (e.MessageType == MessageType.Update && e.Command == Command.OnNewConnection)
            HandleUpdates(e);

        if (e.Command == Command.WhoIAm)
        {
            logger.Debug("Received a who am i response from " + e.NodeId);
            if (!knownNodeIds.Any(x => x.Id == e.NodeId))
                knownNodeIds.Add(new NodeSync(e.NodeId, 0));

            _ = whoIAmSendTime.TryRemove(e.NodeId, out (DateTime time, int count) asda);
            if (e.Parameters != null)
            {
                NewConnectionEstablished?.Invoke(this, (e.NodeId, e.Parameters));
            }

            return;
        }

        NodeSync? known = knownNodeIds.FirstOrDefault(x => x.Id == e.NodeId);
        if (known == default)
        {
            if (!whoIAmSendTime.TryGetValue(e.NodeId, out (DateTime time, int count) dt) || dt.time.Add(waitBeforeWhoIAmSendAgain) > DateTime.Now)
            {
                //SendSingle(e.NodeId, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                if (dt == default)
                    _ = whoIAmSendTime.TryAdd(e.NodeId, (DateTime.Now.Subtract(waitBeforeWhoIAmSendAgain), 0));
                else
                    whoIAmSendTime[e.NodeId] = (DateTime.Now.Subtract(waitBeforeWhoIAmSendAgain), 0);
            }
            if (!queuedMessages.TryGetValue(e.NodeId, out Queue<BinarySmarthomeMessage>? queue))
            {
                queue = new Queue<BinarySmarthomeMessage>();
                queuedMessages.Add(e.NodeId, queue);
            }
            queue.Enqueue(e);
            return;
        }

        if (known.MissedConnections > 0)
        {
            known.MissedConnections = 0;
            ConnectionReestablished?.Invoke(this, (known.Id, e.Parameters));
        }

        if (queuedMessages.TryGetValue(e.NodeId, out Queue<BinarySmarthomeMessage>? messages))
        {
            while (messages.TryDequeue(out BinarySmarthomeMessage? message))
                MessageTypeSwitch(message);
            _ = queuedMessages.Remove(e.NodeId);
        }

        MessageTypeSwitch(e);
    }

    private void MessageTypeSwitch(BinarySmarthomeMessage e)
    {
        switch (e.MessageType)
        {
            case MessageType.Get:
                HandleGets(e);
                break;
            case MessageType.Update:
                HandleUpdates(e);
                break;
            case MessageType.Options:
                HandleOptions(e);
                break;
            default:
                break;
        }
    }

    private bool TryParseSubsRecursive(Sub sub, out List<Sub> subs)
    {
        subs = new List<Sub>();
        try
        {
            subs.Add(sub);
            if (!knownNodeIds.Any(x => x.Id == sub.NodeId))
            {
                if (!whoIAmSendTime.TryGetValue(sub.NodeId, out (DateTime time, int count) dt) /*|| dt.time.Add(waitBeforeWhoIAmSendAgain*2) > DateTime.Now*/)
                {
                    //SendSingle(sub.NodeId, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        _ = whoIAmSendTime.TryAdd(sub.NodeId, (DateTime.Now, 0));
                    else
                        whoIAmSendTime[sub.NodeId] = (DateTime.Now, 0);
                }
            }

            if (sub.Subs is not null and not null)
            {
                foreach (KeyValuePair<uint, Sub> item in sub.Subs)
                {
                    if (TryParseSubsRecursive(item.Value, out List<Sub>? rsubs))
                        subs.AddRange(rsubs);
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e);
            return false;
        }
        return true;
    }

    internal void HandleUpdates(BinarySmarthomeMessage e)
    {
        switch (e.Command)
        {
            case Command.IP:
                break;
            case Command.OnChangedConnections:
            case Command.OnNewConnection:
            case Command.Mesh:

                string? str = Encoding.UTF8.GetString(e.Parameters[0]);
                logger.Debug(str);
                var sub = JsonConvert.DeserializeObject<Sub>(str);
                if (sub is null)
                    return;

                void FillManagedHashSet(Sub s)
                {
                    ownManagedIds.Add(s.NodeId);
                    foreach (var bs in s.Subs)
                        FillManagedHashSet(bs.Value);
                }
                ownManagedIds.Clear();
                FillManagedHashSet(sub);

                RefreshMesh(sub);
                break;
            default:
                SingleUpdateMessageReceived?.Invoke(this, e);
                break;
        }
    }

    internal void RefreshMesh(Sub sub)
    {
        try
        {
            if (sub is null || !TryParseSubsRecursive(sub, out List<Sub>? rsubs))
                return;
            var subs = rsubs.Distinct().ToDictionary(x => x.NodeId, x => x);
            var lostSubs = new List<uint>();

            foreach (NodeSync? item in knownNodeIds) // Check our subs with incoming list
            {
                if (!subs.ContainsKey(item.Id))
                {
                    lostSubs.Add(item.Id);
                }
                else
                {
                    if (item.MissedConnections > 0)
                    {
                        item.MissedConnections = 0;
                        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(item.Id, out AppBroker.Core.Devices.Device? dev))
                            SendSingle((uint)dev.Id, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                        else
                            lostSubs.Add(item.Id);
                    }
                }
            }
            foreach (KeyValuePair<uint, (DateTime time, int count)> item in whoIAmSendTime)
            {
                if (lostSubs.Contains(item.Key))
                {
                    if (subs.ContainsKey((uint)item.Key))
                        _ = lostSubs.Remove(item.Key);
                }
            }
            foreach (uint id in lostSubs)
            {
                NodeSync? knownId = knownNodeIds.FirstOrDefault(x => x.Id == id);
                ConnectionLost?.Invoke(this, id);
                if (knownId != default)
                {
                    if (knownId.MissedConnections > 15)
                        _ = knownNodeIds.Remove(knownId);
                    else
                        knownId.MissedConnections++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex);
        }
    }

    private void HandleGets(BinarySmarthomeMessage e)
    {
        if (e.Command == Command.Time)
            SendTimeUpdate(null);
        else
            SingleGetMessageReceived?.Invoke(this, e);
    }

    private void HandleOptions(BinarySmarthomeMessage e) => SingleOptionsMessageReceived?.Invoke(this, e);

    public void SendSingle<T>(uint destination, T message) where T : BinarySmarthomeMessage
    {
        try
        {
            logger.Debug($"{PackageType.SINGLE}: NodeId: {destination}, Command: {message.Command}, MessageType: {message.MessageType}, ParamsAmount: {message.Parameters.Count}, " + string.Join(", ", message.Parameters.Select(x => BitConverter.ToString(x))));
            using var ms = new MemoryStream();
            message.Serialize(ms);
            ServerSocket.SendToAllClients(PackageType.SINGLE, ms.ToArray(), destination);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }
    public void SendSingle(uint destination, Memory<byte> message)
    {
        try
        {
            ServerSocket.SendToAllClients(PackageType.SINGLE, message, destination, false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }
    //public void SendCustomSingle<T>(long destination, PackageType type, T message)
    //{
    //    try
    //    {
    //        ServerSocket.SendToAllClients(type, message.ToJson(), destination);
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //        logger.Error(e);
    //    }
    //}

    public void SendBroadcast<T>(T message) where T : BinarySmarthomeMessage
    {
        try
        {
            using var ms = new MemoryStream();
            message.Serialize(ms);
            logger.Debug($"{PackageType.BROADCAST}: Command: {message.Command}, MessageType: {message.MessageType}, ParamsAmount: {message.Parameters.Count}, " + string.Join(", ", message.Parameters.Select(x => BitConverter.ToString(x))));
            ServerSocket.SendToAllClients(PackageType.BROADCAST, ms.ToArray());
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }

    public void SendBroadcast(Memory<byte> message)
    {
        try
        {
            ServerSocket.SendToAllClients(PackageType.BROADCAST, message, false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }
    //public void SendCustomBroadcast<T>(PackageType type, T message)
    //{
    //    try
    //    {
    //        ServerSocket.SendToAllClients(type, message.ToJson(), 0);
    //    }
    //    catch (Exception e)
    //    {
    //        Console.WriteLine(e);
    //        logger.Error(e);
    //    }
    //}

    public void SendToBridge<T>(T message) where T : BinarySmarthomeMessage
    {
        try
        {
            using var ms = new MemoryStream();
            message.Serialize(ms);
            logger.Debug($"{PackageType.BRIDGE}: Command: {message.Command}, MessageType: {message.MessageType}, ParamsAmount: {message.Parameters.Count}, " + string.Join(", ", message.Parameters.Select(x => BitConverter.ToString(x))));

            ServerSocket.SendToAllClients(PackageType.BRIDGE, ms.ToArray(), nodeId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }

    private void TryCatchWhoAmITask(object? _)
    {
        try
        {
            clients.RemoveAll(x => !x.Connected);
            if (ConnectedClients > 0)
                WhoAmITask();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            logger.Error(ex);
        }
    }

    private void WhoAmITask()
    {
        var toDelete = new Dictionary<uint, (DateTime time, int count)>();
        try
        {
            foreach (KeyValuePair<uint, (DateTime time, int count)> item in whoIAmSendTime)
            {
                if (item.Value.time.Add(waitBeforeWhoIAmSendAgain) < DateTime.Now)
                {
                    SendSingle(item.Key, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    whoIAmSendTime[item.Key] = (DateTime.Now, item.Value.count + 1);
                }
                if (item.Value.count > 20)
                    toDelete.Add(item.Key, item.Value);
            }

            foreach (KeyValuePair<uint, (DateTime time, int count)> item in toDelete)
            {
                _ = whoIAmSendTime.TryRemove(item.Key, out (DateTime time, int count) val);
            }
            toDelete.Clear();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            logger.Error(e);
        }
    }

    public void Dispose()
    {
        foreach (Timer? item in timers)
        {
            item.Dispose();
        }
    }
}

