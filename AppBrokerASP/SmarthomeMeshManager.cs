using System.Net;
using System.Text;
using System.Threading;

using AppBrokerASP.Devices.Painless;

using Newtonsoft.Json;

using PainlessMesh;

namespace AppBrokerASP
{

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

        public event EventHandler<BinarySmarthomeMessage>? SingleUpdateMessageReceived;
        public event EventHandler<BinarySmarthomeMessage>? SingleOptionsMessageReceived;
        public event EventHandler<BinarySmarthomeMessage>? SingleGetMessageReceived;
        public event EventHandler<(Sub, ByteLengthList)>? NewConnectionEstablished;
        public event EventHandler<uint>? ConnectionLost;
        public event EventHandler<(uint id, ByteLengthList parameter)>? ConnectionReastablished;

        private static readonly ServerSocket serverSocket = new();

        private readonly TimeSpan WaitBeforeWhoIAmSendAgain;
        private readonly List<NodeSync> knownNodeIds;
        private readonly int listenPort;
        private readonly ConcurrentDictionary<uint, (DateTime time, int count)> WhoIAmSendTime;

        private readonly uint nodeID = 1;
        private readonly Dictionary<uint, Queue<BinarySmarthomeMessage>> queuedMessages;
        private readonly List<Timer> timers;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private bool running;

        public SmarthomeMeshManager(int listenPort, uint nodeId = 1)
        {
            WaitBeforeWhoIAmSendAgain = new TimeSpan(0, 0, 10);

            queuedMessages = new();
            timers = new();
            nodeID = nodeId;
            WhoIAmSendTime = new();
            knownNodeIds = new() { new NodeSync(nodeID, 0), new NodeSync(0, 0) };
            this.listenPort = listenPort;

        }

        public void Start()
        {
            if (running)
                return;
            running = true;
            serverSocket.OnClientConnected += ServerSocket_OnClientConnected;
            serverSocket.Start(new IPAddress(new byte[] { 0, 0, 0, 0 }), listenPort);

            timers.Add(new Timer(TryCatchWhoAmITask, null, TimeSpan.FromSeconds(1.0), WaitBeforeWhoIAmSendAgain));
            timers.Add(new Timer(SendTimeUpdate, null, TimeSpan.FromMinutes(1.0), TimeSpan.FromHours(1d)));
            timers.Add(new Timer(GetMeshUpdate, null, TimeSpan.FromSeconds(10.0), TimeSpan.FromSeconds(10d)));
            InstanceContainer.UpdateManager.Advertisment += OtaAdvertisment;
        }

        public void Stop()
        {
            if (!running)
                return;
            running = false;
            serverSocket.Stop();
            serverSocket.OnClientConnected -= ServerSocket_OnClientConnected;

            foreach (var timer in timers)
            {
                timer.Dispose();
            }
            timers.Clear();
        }

        private void GetMeshUpdate(object? state)
        {
            SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
        }

        internal void UpdateTime() => SendTimeUpdate(null);

        private static readonly byte[] fromServer = new byte[] { 1 };
        private void SendTimeUpdate(object? state)
        {
            var dto = new DateTimeOffset(DateTime.Now.Ticks, TimeSpan.Zero);
            var list = new ByteLengthList(fromServer, BitConverter.GetBytes((int)dto.ToUnixTimeSeconds()));
            var msg = new BinarySmarthomeMessage(0, MessageType.Update, Command.Time, list);
            SendBroadcast(msg);
        }


        private void OtaAdvertisment(object? sender, PainlessMesh.Ota.FirmwareMetadata e)
        {
            foreach (var item in InstanceContainer.DeviceManager.Devices.Values)
            {
                if (item is PainlessDevice pd)
                    pd.OtaAdvertisment(e);
            }
        }

        private void ServerSocket_OnClientConnected(object? sender, BaseClient baseClient)
        {
            baseClient.ReceivedData += SocketClientDataReceived;
            SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
        }

        public void SocketClientDataReceived(object? sender, BinarySmarthomeMessage e)
        {
            //var bc = (BaseClient)sender;

            if (e.MessageType == MessageType.Update && e.Command == Command.OnNewConnection)
                HandleUpdates(e);

            if (e.Command == Command.WhoIAm)
            {
                if (!knownNodeIds.Any(x => x.Id == e.NodeId))
                    knownNodeIds.Add(new NodeSync(e.NodeId, 0));

                _ = WhoIAmSendTime.TryRemove(e.NodeId, out var asda);
                if (e.Parameters != null)
                    NewConnectionEstablished?.Invoke(this, (new Sub { NodeId = e.NodeId }, e.Parameters));
                return;
            }

            var known = knownNodeIds.FirstOrDefault(x => x.Id == e.NodeId);
            if (known == default)
            {
                if (!WhoIAmSendTime.TryGetValue(e.NodeId, out var dt) || dt.time.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                {
                    //SendSingle(e.NodeId, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        _ = WhoIAmSendTime.TryAdd(e.NodeId, (DateTime.Now.Subtract(WaitBeforeWhoIAmSendAgain), 0));
                    else
                        WhoIAmSendTime[e.NodeId] = (DateTime.Now.Subtract(WaitBeforeWhoIAmSendAgain), 0);
                }
                if (!queuedMessages.TryGetValue(e.NodeId, out var queue))
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
                ConnectionReastablished?.Invoke(this, (known.Id, e.Parameters));
            }

            if (queuedMessages.TryGetValue(e.NodeId, out var messages))
            {
                while (messages.TryDequeue(out var message))
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
                    if (!WhoIAmSendTime.TryGetValue(sub.NodeId, out var dt) /*|| dt.time.Add(WaitBeforeWhoIAmSendAgain*2) > DateTime.Now*/)
                    {
                        //SendSingle(sub.NodeId, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                        if (dt == default)
                            _ = WhoIAmSendTime.TryAdd(sub.NodeId, (DateTime.Now, 0));
                        else
                            WhoIAmSendTime[sub.NodeId] = (DateTime.Now, 0);
                    }
                }

                if (sub.Subs is not null and not null)
                    foreach (var item in sub.Subs)
                    {
                        if (TryParseSubsRecursive(item.Value, out var rsubs))
                            subs.AddRange(rsubs);
                    }

            }
            catch (Exception e)
            {
                logger.Error(e);
                return false;
            }
            return true;
        }

        private void HandleUpdates(BinarySmarthomeMessage e)
        {
            switch (e.Command)
            {
                case Command.IP:
                    break;
                case Command.OnChangedConnections:
                case Command.OnNewConnection:
                case Command.Mesh:
                    RefreshMesh(e);
                    break;
                default:
                    SingleUpdateMessageReceived?.Invoke(this, e);
                    break;
            }
        }

        private void RefreshMesh(BinarySmarthomeMessage e)
        {
            try
            {
                var str = Encoding.UTF8.GetString(e.Parameters[0]);
                logger.Debug(str);
                var sub = JsonConvert.DeserializeObject<Sub>(str);
                if (sub is null || !TryParseSubsRecursive(sub, out var rsubs))
                    return;
                var subs = rsubs.Distinct().ToDictionary(x => x.NodeId, x => x);
                var lostSubs = new List<uint>();

                foreach (var item in knownNodeIds) // Check our subs with incoming list
                {
                    if (!subs.ContainsKey(item.Id))
                        lostSubs.Add(item.Id);
                    else
                    {
                        if (item.MissedConnections > 0)
                        {
                            item.MissedConnections = 0;
                            if (InstanceContainer.DeviceManager.Devices.TryGetValue(item.Id, out var dev))
                                SendSingle((uint)dev.Id, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                            else
                                lostSubs.Add(item.Id);
                        }
                    }
                }
                foreach (var item in WhoIAmSendTime)
                {
                    if (lostSubs.Contains(item.Key))
                    {
                        if (subs.ContainsKey((uint)item.Key))
                            _ = lostSubs.Remove(item.Key);
                    }
                }
                foreach (var id in lostSubs)
                {
                    var knownId = knownNodeIds.FirstOrDefault(x => x.Id == id);
                    ConnectionLost?.Invoke(this, id);
                    if (knownId != default)
                        if (knownId.MissedConnections > 15)
                            _ = knownNodeIds.Remove(knownId);
                        else
                            knownId.MissedConnections++;
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
                serverSocket.SendToAllClients(PackageType.SINGLE, ms.ToArray(), destination);
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
                serverSocket.SendToAllClients(PackageType.SINGLE, message, destination, false);
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
        //        serverSocket.SendToAllClients(type, message.ToJson(), destination);
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
                serverSocket.SendToAllClients(PackageType.BROADCAST, ms.ToArray());
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
                serverSocket.SendToAllClients(PackageType.BROADCAST, message, false);
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
        //        serverSocket.SendToAllClients(type, message.ToJson(), 0);
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

                serverSocket.SendToAllClients(PackageType.BRIDGE, ms.ToArray(), nodeID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.Error(e);
            }
        }

        private void TryCatchWhoAmITask(object? o)
        {
            try
            {
                WhoAmITask(o);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                logger.Error(ex);
            }
        }

        private void WhoAmITask(object? o)
        {
            var toDelete = new Dictionary<uint, (DateTime time, int count)>();
            try
            {
                foreach (var item in WhoIAmSendTime)
                {
                    if (item.Value.time.Add(WaitBeforeWhoIAmSendAgain) < DateTime.Now)
                    {
                        SendSingle(item.Key, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                        WhoIAmSendTime[item.Key] = (DateTime.Now, item.Value.count + 1);
                    }
                    if (item.Value.count > 20)
                        toDelete.Add(item.Key, item.Value);
                }

                foreach (var item in toDelete)
                {
                    _ = WhoIAmSendTime.TryRemove(item.Key, out var val);
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
            foreach (var item in timers)
            {
                item.Dispose();
            }
        }
    }
}

