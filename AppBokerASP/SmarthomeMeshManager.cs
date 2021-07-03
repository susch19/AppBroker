﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using PainlessMesh;

namespace AppBokerASP
{

    public class SmarthomeMeshManager: IDisposable
    {
        private class NodeSync
        {
            public long Id { get; }
            public byte MissedConnections { get; set; }
            public NodeSync(long id, byte missedConnections)
            {
                Id = id;
                MissedConnections = missedConnections;
            }
        }

        public event EventHandler<BinarySmarthomeMessage> SingleUpdateMessageReceived;
        public event EventHandler<BinarySmarthomeMessage> SingleOptionsMessageReceived;
        public event EventHandler<BinarySmarthomeMessage> SingleGetMessageReceived;
        public event EventHandler<(Sub, ByteLengthList)> NewConnectionEstablished;
        public event EventHandler<long> ConnectionLost;
        public event EventHandler<(long id, ByteLengthList parameter)> ConnectionReastablished;

        private static readonly ServerSocket serverSocket = new();

        private readonly TimeSpan WaitBeforeWhoIAmSendAgain;
        private readonly List<NodeSync> knownNodeIds;
        private readonly ConcurrentDictionary<long, (DateTime time, int count)> WhoIAmSendTime;

        private readonly long nodeID = 1;
        private readonly Dictionary<long, Queue<BinarySmarthomeMessage>> queuedMessages;
        private readonly List<Timer> timers;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();


        public SmarthomeMeshManager(int listenPort, long nodeId = 1)
        {
            WaitBeforeWhoIAmSendAgain = new TimeSpan(0, 0, 30);

            queuedMessages = new Dictionary<long, Queue<BinarySmarthomeMessage>>();
            timers = new List<Timer>();
            nodeID = nodeId;
            WhoIAmSendTime = new ConcurrentDictionary<long, (DateTime, int)>();
            knownNodeIds = new List<NodeSync> { new NodeSync(nodeID, 0), new NodeSync(0, 0) };

            serverSocket.OnClientConnected += ServerSocket_OnClientConnected;
            serverSocket.Start(new IPAddress(new byte[] { 0, 0, 0, 0 }), listenPort);

            timers.Add(new Timer(TryCatchWhoAmITask, null, TimeSpan.FromSeconds(20d), WaitBeforeWhoIAmSendAgain));
            timers.Add(new Timer(SendTimeUpdate, null, TimeSpan.FromMinutes(1d), TimeSpan.FromHours(1d)));
            timers.Add(new Timer(GetMeshUpdate, null, TimeSpan.FromSeconds(10d), TimeSpan.FromMinutes(1d)));

        }

        private void GetMeshUpdate(object? state)
        {
            SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
        }

        internal void UpdateTime() => SendTimeUpdate(null);
        private void SendTimeUpdate(object? state)
        {
            var msg = new BinarySmarthomeMessage(0, MessageType.Update, Command.Time, Encoding.UTF8.GetBytes($"{{\"Date\":\"{DateTime.Now:dd.MM.yyyy HH:mm:ss}\"}}"));
            SendBroadcast(msg);
        }

        private void ServerSocket_OnClientConnected(object sender, BaseClient baseClient)
        {
            baseClient.ReceivedData += SocketClientDataReceived;
            SendToBridge(new BinarySmarthomeMessage(1, MessageType.Get, Command.Mesh));
        }

        public void SocketClientDataReceived(object sender, BinarySmarthomeMessage e)
        {
            //var bc = (BaseClient)sender;

            if (e.MessageType == MessageType.Update && e.Command == Command.OnNewConnection)
                HandleUpdates(e);

            if (e.Command == Command.WhoIAm)
            {
                if (!knownNodeIds.Any(x => x.Id == e.NodeId))
                    knownNodeIds.Add(new NodeSync(e.NodeId, 0));

                WhoIAmSendTime.TryRemove(e.NodeId, out var asda);
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
                        WhoIAmSendTime.TryAdd(e.NodeId, (DateTime.Now.Subtract(WaitBeforeWhoIAmSendAgain), 0));
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
                queuedMessages.Remove(e.NodeId);
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
                    if (!WhoIAmSendTime.TryGetValue(sub.NodeId, out var dt) || dt.time.Add(WaitBeforeWhoIAmSendAgain) > DateTime.Now)
                    {
                        //SendSingle(sub.NodeId, new BinarySmarthomeMessage(0, MessageType.Get, Command.WhoIAm));
                        if (dt == default)
                            WhoIAmSendTime.TryAdd(sub.NodeId, (DateTime.Now, 0));
                        else
                            WhoIAmSendTime[sub.NodeId] = (DateTime.Now, 0);
                    }
                }

                if (sub.Subs != null && sub.Subs != default)
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
                var sub = JsonSerializer.Deserialize<Sub>(str);
                if (!TryParseSubsRecursive(sub, out var rsubs))
                    return;
                var subs = rsubs.Distinct().ToDictionary(x => x.NodeId, x => x);
                var lostSubs = new List<long>();

                foreach (var item in knownNodeIds) // Check our subs with incoming list
                {
                    if (!subs.ContainsKey((uint)item.Id))
                        lostSubs.Add(item.Id);
                    else
                    {
                        if (item.MissedConnections > 0)
                        {
                            item.MissedConnections = 0;
                            if (Program.DeviceManager.Devices.TryGetValue(item.Id, out var dev))
                                dev.Reconnect(null);
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
                            lostSubs.Remove(item.Key);
                    }
                }
                foreach (var id in lostSubs)
                {
                    var knownId = knownNodeIds.FirstOrDefault(x => x.Id == id);
                    ConnectionLost?.Invoke(this, id);
                    if (knownId != default)
                        if (knownId.MissedConnections > 15)
                            knownNodeIds.Remove(knownId);
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

        public void SendSingle<T>(long destination, T message) where T : BinarySmarthomeMessage
        {
            try
            {
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

        public void SendBroadcast<T>(T message) where T : BinarySmarthomeMessage
        {
            try
            {
                using var ms = new MemoryStream();
                message.Serialize(ms);
                serverSocket.SendToAllClients(PackageType.BROADCAST, ms.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.Error(e);
            }
        }

        public void SendToBridge<T>(T message) where T : BinarySmarthomeMessage
        {
            try
            {
                using var ms = new MemoryStream();
                message.Serialize(ms);
                serverSocket.SendToAllClients(PackageType.BRIDGE, ms.ToArray(), nodeID);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                logger.Error(e);
            }
        }

        private void TryCatchWhoAmITask(object o)
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

        private void WhoAmITask(object o)
        {
            var toDelete = new Dictionary<long, (DateTime time, int count)>();
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
                    WhoIAmSendTime.TryRemove(item.Key, out var val);
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

