using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PainlessMesh
{
    public class Node : Connection
    {

        public event EventHandler<GeneralSmarthomeMessage> SingleMessageReceived;
        public event EventHandler<GeneralSmarthomeMessage> BroadcastMessageReceived;
        public event EventHandler<(Connection, List<string>)> NewConnectionEstablished;

        private readonly Action<int, string> Receive;
        private uint adjustTime = 0;
        private readonly DateTime startDate = DateTime.Now;

        public Node(uint id) : base()
        {
            NodeId = id;
            InitMaintenance();
        }

        public async Task<TcpClient> ConnectTCPAsync(string host, ushort port)
        {
            Client = new TcpClient();
            await Client.ConnectAsync(host, port);
            StartListening();

            return Client;
        }

        public TcpListener ListenTCPAsync(ushort port)
        {
            var listener = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), port);
            listener.Start();
            listener.BeginAcceptTcpClient(InitConnection, null);
            return listener;
        }

        private void InitConnection(IAsyncResult ar) => StartListening();

        public void StartListening()
        {
            uint fromId = 0;

            new Task(() =>
            {
                try
                {
                    var ser = JsonSerializer.Create();

                    while (Client != null && Client.Connected == true)
                    {
                        JObject json;
                        if (Client.Available == 0)
                        {
                            if (!Client.Connected)
                                break;
                            Thread.Sleep(50);
                            continue;
                        }


                        using (var sr = new StreamReader(Client.GetStream(), Encoding.ASCII, false, 2048, true))
                        {
                            var whatsWrong = sr.ReadLine();
                            Console.WriteLine(whatsWrong);
                            json = JsonConvert.DeserializeObject<JObject>(whatsWrong);

                            //using (var jr = new JsonTextReader(sr))
                            //{
                            //    json = ser.Deserialize<JObject>(jr);
                            //}
                        }


                        var bm = json.ToObject<BasicMessage>();

                        if (!Connections.TryGetValue(bm.from, out var conn))
                        {
                            if (bm.type == PackageType.BROADCAST)
                            {
                                var broadcastMessage = json.ToObject<BroadcastMessage>();
                                if (string.IsNullOrWhiteSpace(broadcastMessage.msg) || broadcastMessage.msg[0] != '{')
                                    continue;

                                var mt = JsonConvert.DeserializeObject<SmarthomeCommand>(broadcastMessage.msg);
                                if (mt.Command == "WhoIAm")
                                {
                                    fromId = bm.from;
                                    Connection meshConnection = new Connection()
                                    {
                                        NodeId = fromId,
                                        Client = Client
                                    };
                                    Connections[fromId] = meshConnection;
                                    NewConnectionEstablished?.Invoke(this, (meshConnection, mt.Parameters));
                                    SendSingle(fromId, "");
                                }
                            }

                        }

                        if (Connections.TryGetValue(fromId, out var con))
                        {
                            switch (bm.type)
                            {
                                case PackageType.DROP:
                                    break;
                                case PackageType.TIME_SYNC:
                                    HandleMessage(json.ToObject<TimeSync>());
                                    break;
                                case PackageType.NODE_SYNC_REQUEST:
                                case PackageType.NODE_SYNC_REPLY:
                                    HandleMessage(json.ToObject<NodeSyncMessage>());
                                    break;
                                case PackageType.BROADCAST:
                                    HandleMessage(json.ToObject<BroadcastMessage>());
                                    break;
                                case PackageType.SINGLE:
                                    HandleMessage(json.ToObject<SingleAdressedMessage>());
                                    break;
                                default:
                                    break;
                            }

                            con.LastReceived = DateTime.Now;
                        }
                    }
                    Connections.Remove(fromId);
                    Client = null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Connections.Remove(fromId);
                    Client = null;
                }
            }
            ).Start();

        }

        private void HandleMessage(NodeSyncMessage nodeSyncMessage)
        {
            Connections[nodeSyncMessage.from].Connections = nodeSyncMessage.subs;

            if (nodeSyncMessage.type == PackageType.NODE_SYNC_REQUEST)
                SendNodeSync(nodeSyncMessage.from, false);
        }

        private void HandleMessage(BroadcastMessage broadcastMessage)
            => BroadcastMessageReceived?.Invoke(this, JsonConvert.DeserializeObject<GeneralSmarthomeMessage>(broadcastMessage.msg));

        private void HandleMessage(SingleAdressedMessage singleAdressedMessage)
            => SingleMessageReceived?.Invoke(this, JsonConvert.DeserializeObject<GeneralSmarthomeMessage>(singleAdressedMessage.msg));

        private uint nodeTime() => ((uint)((DateTime.Now - startDate).Ticks * TimeSpan.TicksPerMillisecond) + adjustTime);

        private void HandleMessage(TimeSync timeSync)
        {
            var timeReceived = nodeTime();
            var msg = timeSync.msg;
            if (msg.type != 2)
            {
                timeSync.dest = timeSync.from;
                timeSync.from = NodeId;

                if (msg.type == 0)
                {
                    msg.t0 = nodeTime();
                }
                else if (msg.type == 1)
                {
                    msg.t1 = timeReceived;
                    msg.t2 = nodeTime();
                }
                ++msg.type;

                SafeWrite(timeSync.dest, timeSync);
            }
            else
            {
                adjustTime += ((msg.t1 - msg.t0) / 2 + (msg.t2 - timeReceived) / 2);
            }
        }

        public bool SendNodeSync(uint fromId, bool request = true)
        {
            if (Connections.TryGetValue(fromId, out var con))
            {
                NodeSyncMessage nsm = new NodeSyncMessage()
                {
                    dest = fromId,
                    from = NodeId,
                    type = request ? PackageType.NODE_SYNC_REQUEST : PackageType.NODE_SYNC_REPLY

                };


                //pack["subs"] = node.connections.byPair.filter!((t) => t[0] != fromID).map!((t) => t[1]).array.toJSON;
                return SafeWrite(fromId, nsm);
            }
            return false;


        }

        public bool SendSingle(uint destination, string message)
        {
            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections.TryGetValue(destination, out var con))
                {
                    var msg = new SingleAdressedMessage() { dest = destination, from = NodeId, msg = message, type = PackageType.SINGLE };
                    SafeWrite(destination, msg);
                    return true;
                }
            }

            return false;
        }


        public void InitMaintenance()
        {
            var ct = new CancellationToken(false);
            new Task(() =>
            {
                for (; ; )
                {
                    foreach (var client in Connections.ToList())
                    {
                        if (!SendNodeSync(client.Value.NodeId))
                        {
                            RemoveConnection(client.Value);
                        }
                    }
                    Thread.Sleep(6000);
                }
            }, ct).Start();
        }
    }
}
