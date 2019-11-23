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
using PainlessMesh;

namespace AppBokerASP
{
    public class SmarthomeMeshManager
    {
        private TimeSpan WaitBeforeWhoIAmSendAgain = new TimeSpan(0, 0, 30);

        public event EventHandler<GeneralSmarthomeMessage> SingleUpdateMessageReceived;
        public event EventHandler<GeneralSmarthomeMessage> SingleGetMessageReceived;
        public event EventHandler<(Sub, List<string>)> NewConnectionEstablished;

        private static ServerSocket serverSocket = new ServerSocket();

        private readonly List<uint> knownNodeIds;
        private readonly ConcurrentDictionary<uint, DateTime> WhoIAmSendTime;

        private readonly uint nodeID = 1;


        private Timer timeBroadcastTimer;
        private Timer whoAmITask;
        private Timer getMeshTreeUpdateTask;

        public SmarthomeMeshManager(int listenPort, uint nodeId = 1)
        {
            nodeID = nodeId;
            WhoIAmSendTime = new ConcurrentDictionary<uint, DateTime>();
            knownNodeIds = new List<uint> { nodeID };
            serverSocket.OnClientConnected += ServerSocket_OnClientConnected;
            serverSocket.Start(new IPAddress(new byte[] { 0, 0, 0, 0 }), listenPort);
            whoAmITask = new Timer(WhoAmITask, null, TimeSpan.FromSeconds(10d), WaitBeforeWhoIAmSendAgain);
            timeBroadcastTimer = new Timer((n) => SendBroadcast(new GeneralSmarthomeMessage(0, MessageType.Update, Command.Time, JsonSerializer.Deserialize<JsonElement>($"{{\"Date\":\"{DateTime.Now.ToString()}\"}}"))), null, TimeSpan.FromMinutes(1d), TimeSpan.FromDays(1d));
            getMeshTreeUpdateTask = new Timer((n) => SendToBridge(new GeneralSmarthomeMessage(1, MessageType.Get, Command.Mesh, JsonSerializer.Deserialize<JsonElement>("{}"))), null, TimeSpan.FromSeconds(20d), TimeSpan.FromMinutes(1d));
        }

        private void ServerSocket_OnClientConnected(object sender, BaseClient baseClient)
        {
            //baseClient.Send(PackageType.BRIDGE, "GetMeshTree", nodeID);
            SendToBridge(new GeneralSmarthomeMessage(1, MessageType.Get, Command.Mesh, JsonSerializer.Deserialize<JsonElement>("{}")));
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
                if (!WhoIAmSendTime.TryGetValue(e.NodeId, out var dt) || dt.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                {
                    //SendSingle(e.NodeId, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        while (!WhoIAmSendTime.TryAdd(e.NodeId, DateTime.Now)) { }
                    else
                        WhoIAmSendTime[e.NodeId] = DateTime.Now;
                }
                return;
            }


            switch (e.MessageType)
            {
                case MessageType.Get:
                    HandleGets(e);
                    break;
                case MessageType.Update:
                    HandleUpdates(e);
                    break;
                default:
                    break;
            }
        }

        private void ParseSubs(Sub sub)
        {
            if (!knownNodeIds.Contains(sub.NodeId))
            {
                if (!WhoIAmSendTime.TryGetValue(sub.NodeId, out var dt) || dt.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                {
                    //SendSingle(sub.NodeId, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    if (dt == default)
                        while (!WhoIAmSendTime.TryAdd(sub.NodeId, DateTime.Now)) { }
                    else
                        WhoIAmSendTime[sub.NodeId] = DateTime.Now;
                }
            }
            if (sub.Subs != null && sub.Subs != default)
                foreach (var item in sub.Subs)
                    ParseSubs(item.Value);
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
                    ParseSubs(sub);
                    break;
                default:
                    SingleUpdateMessageReceived?.Invoke(this, e);
                    break;
            }

        }

        private void HandleGets(GeneralSmarthomeMessage e)
        {
            if (e.Command == Command.Time)
                SendSingle(e.NodeId, new GeneralSmarthomeMessage(e.NodeId, MessageType.Update, Command.Time, JsonSerializer.Deserialize<JsonElement>($"{{\"Date\":\"{DateTime.Now.ToString()}\"}}")));
            else
                SingleGetMessageReceived?.Invoke(this, e);
        }

        public void SendSingle<T>(uint destination, T message)
            => serverSocket.SendToAllClients(PackageType.SINGLE, message.ToJson(), destination);

        public void SendBroadcast<T>(T message)
            => serverSocket.SendToAllClients(PackageType.BROADCAST, message.ToJson());

        public void SendToBridge<T>(T message)
            => serverSocket.SendToAllClients(PackageType.BRIDGE, message.ToJson(), nodeID);

        private void WhoAmITask(object o)
        {

            foreach (var item in WhoIAmSendTime)
            {
                if (item.Value.Add(WaitBeforeWhoIAmSendAgain) < DateTime.Now)
                {
                    SendSingle(item.Key, new GeneralSmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                    WhoIAmSendTime[item.Key] = DateTime.Now;
                }
            }

        }

    }
}

