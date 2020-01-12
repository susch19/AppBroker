using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PainlessMesh;

namespace AppBokerASP
{
    public class SmarthomeMeshManager
    {

        public event EventHandler<GeneralSmarthomeMessage> SingleUpdateMessageReceived;
        public event EventHandler<GeneralSmarthomeMessage> SingleOptionsMessageReceived;
        public event EventHandler<GeneralSmarthomeMessage> SingleGetMessageReceived;
        public event EventHandler<(Sub, List<string>)> NewConnectionEstablished;
        public event EventHandler<uint> ConnectionLost;

        private static ServerSocket serverSocket = new ServerSocket();

        private readonly TimeSpan WaitBeforeWhoIAmSendAgain;
        private readonly List<uint> knownNodeIds;
        private readonly ConcurrentDictionary<uint, (DateTime time, int count)> WhoIAmSendTime;

        private readonly uint nodeID = 1;
        private Dictionary<uint, Queue<GeneralSmarthomeMessage>> queuedMessages;

        public List<Timer> Timer;


        public SmarthomeMeshManager(int listenPort, uint nodeId = 1)
        {
            WaitBeforeWhoIAmSendAgain = new TimeSpan(0, 0, 30);
            queuedMessages = new Dictionary<uint, Queue<GeneralSmarthomeMessage>>();
            Timer = new List<Timer>();
            nodeID = nodeId;
            WhoIAmSendTime = new ConcurrentDictionary<uint, (DateTime, int)>();
            knownNodeIds = new List<uint> { nodeID, 0 };
            serverSocket.OnClientConnected += ServerSocket_OnClientConnected;
            serverSocket.Start(new IPAddress(new byte[] { 0, 0, 0, 0 }), listenPort);
            Timer.Add(new Timer(WhoAmITask, null, TimeSpan.FromSeconds(10d), WaitBeforeWhoIAmSendAgain));
            Timer.Add(new Timer((n) => SendBroadcast(new GeneralSmarthomeMessage(0, MessageType.Update, Command.Time, $"{{\"Date\":\"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}\"}}".ToJToken())), null, TimeSpan.FromMinutes(1d), TimeSpan.FromHours(1d)));
            Timer.Add(new Timer((n) => SendToBridge(new GeneralSmarthomeMessage(1, MessageType.Get, Command.Mesh)), null, TimeSpan.FromSeconds(20d), TimeSpan.FromMinutes(1d)));
        }

        private void ServerSocket_OnClientConnected(object sender, BaseClient baseClient)
        {
            //baseClient.Send(PackageType.BRIDGE, "GetMeshTree", nodeID);
            SendToBridge(new GeneralSmarthomeMessage(1, MessageType.Get, Command.Mesh));
            baseClient.ReceivedData += SocketClientDataReceived;
            //baseClient.Start();
        }

        private void SocketClientDataReceived(object sender, GeneralSmarthomeMessage e)
        {
            var bc = (BaseClient)sender;

            if (e.MessageType == MessageType.Update && e.Command == Command.OnNewConnection)
                HandleUpdates(e);

            if (e.Command == Command.WhoIAm)
            {
                knownNodeIds.Add(e.NodeId);
                while (WhoIAmSendTime.ContainsKey(e.NodeId) && !WhoIAmSendTime.TryRemove(e.NodeId, out var asda)) { }
                if (e.Parameters == null)
                    return;
                NewConnectionEstablished?.Invoke(this, (new Sub { NodeId = e.NodeId }, e.Parameters?.ToStringArray().ToList()));
                return;
            }

            if (!knownNodeIds.Contains(e.NodeId))
            {
                if (!WhoIAmSendTime.TryGetValue(e.NodeId, out var dt) || dt.time.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                {
                    //SendSingle(e.NodeId, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        while (!WhoIAmSendTime.TryAdd(e.NodeId, (DateTime.Now, 0))) { }
                    else
                        WhoIAmSendTime[e.NodeId] = (DateTime.Now, 0);
                }
                if (!queuedMessages.TryGetValue(e.NodeId, out var queue))
                {
                    queue = new Queue<GeneralSmarthomeMessage>();
                    queuedMessages.Add(e.NodeId, queue);
                }
                queue.Enqueue(e);
                return;
            }

            if (queuedMessages.TryGetValue(e.NodeId, out var messages))
            {
                while (messages.TryDequeue(out var message))
                    switch (message.MessageType)
                    {
                        case MessageType.Get:
                            HandleGets(message);
                            break;
                        case MessageType.Update:
                            HandleUpdates(message);
                            break;
                        case MessageType.Options:
                            HandleOptions(message);
                            break;
                        default:
                            break;
                    }
                queuedMessages.Remove(e.NodeId);
            }

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

        internal void UpdateTime()
        => SendBroadcast(new GeneralSmarthomeMessage(0, MessageType.Update, Command.Time, $"{{\"Date\":\"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}\"}}".ToJToken()));


        private List<Sub> ParseSubsRecursive(Sub sub)
        {
            var subs = new List<Sub>();
            subs.Add(sub);
            if (!knownNodeIds.Contains(sub.NodeId))
            {
                if (!WhoIAmSendTime.TryGetValue(sub.NodeId, out var dt) || dt.time.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                {
                    //SendSingle(sub.NodeId, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        while (!WhoIAmSendTime.TryAdd(sub.NodeId, (DateTime.Now, 0))) { }
                    else
                        WhoIAmSendTime[sub.NodeId] = (DateTime.Now, 0);
                }
            }

            if (sub.Subs != null && sub.Subs != default)
                foreach (var item in sub.Subs)
                    subs.AddRange(ParseSubsRecursive(item.Value));
            return subs;
        }


        private void HandleUpdates(GeneralSmarthomeMessage e)
        {
            switch (e.Command)
            {
                case Command.IP:
                    break;
                case Command.OnChangedConnections:
                case Command.OnNewConnection:
                case Command.Mesh:

                    var sub = e.Parameters[0].ToObject<Sub>();
                    var subs = ParseSubsRecursive(sub).ToDictionary(x => x.NodeId, x => x);
                    var subIds = new List<uint>();

                    foreach (var item in knownNodeIds)
                    {
                        if (!subs.ContainsKey(item))
                            subIds.Add(item);
                    }
                    foreach (var item in WhoIAmSendTime)
                    {
                        if (subIds.Contains(item.Key))
                        {
                            if (subs.ContainsKey(item.Key))
                                subIds.Remove(item.Key);
                        }
                    }
                    foreach (var id in subIds)
                    {
                        ConnectionLost?.Invoke(this, id);
                        knownNodeIds.Remove(id);
                    }
                    break;
                default:
                    SingleUpdateMessageReceived?.Invoke(this, e);
                    break;
            }

        }

        private void HandleGets(GeneralSmarthomeMessage e)
        {
            if (e.Command == Command.Time)
                SendBroadcast(new GeneralSmarthomeMessage(e.NodeId, MessageType.Update, Command.Time, $"{{\"Date\":\"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}\"}}".ToJToken()));
            else
                SingleGetMessageReceived?.Invoke(this, e);
        }

        private void HandleOptions(GeneralSmarthomeMessage e)
        {
            SingleOptionsMessageReceived?.Invoke(this, e);
        }

        public void SendSingle<T>(uint destination, T message)
        {
            try
            {
                serverSocket.SendToAllClients(PackageType.SINGLE, message.ToJson(), destination);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendBroadcast<T>(T message)
        {
            try
            {
                serverSocket.SendToAllClients(PackageType.BROADCAST, message.ToJson());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void SendToBridge<T>(T message)
        {
            try
            {
                serverSocket.SendToAllClients(PackageType.BRIDGE, message.ToJson(), nodeID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void WhoAmITask(object o)
        {
            var toDelete = new Dictionary<uint, (DateTime time, int count)>();
            try
            {
                foreach (var item in WhoIAmSendTime)
                {
                    if (item.Value.time.Add(WaitBeforeWhoIAmSendAgain) < DateTime.Now)
                    {
                        SendSingle(item.Key, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                        WhoIAmSendTime[item.Key] = (DateTime.Now, item.Value.count + 1);
                    }
                    if (item.Value.count > 20)
                        toDelete.Add(item.Key, item.Value);
                }

                foreach (var item in toDelete)
                {
                    while (!WhoIAmSendTime.TryRemove(item.Key, out var val))
                        Thread.Sleep(1);
                }
                toDelete.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }
}

