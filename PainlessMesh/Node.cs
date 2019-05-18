using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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

        //public async Task<TcpClient> ConnectTCPAsync(string host, ushort port)
        //{
        //    Client = new TcpClient();
        //    SerialPort = new SerialPort("COM5", 512000);

        //    await Client.ConnectAsync(host, port);
        //    StartListening();
        //    var getNodeIdsMessage = new BroadcastMessage { dest = 0, from = NodeId, type = PackageType.BROADCAST, msg = "ServerStarted" };
        //    var getNodeIds = JsonConvert.SerializeObject(getNodeIdsMessage) + '\0';
        //    Client.Client.Send(Encoding.UTF8.GetBytes(getNodeIds));
        //    SerialPort.Write(getNodeIds);

        //    return Client;
        //}
        public SerialPort ConnectSerial(string port, int baudrate)
        {

            SerialPort = new SerialPort(port, baudrate);
            if (SerialPort == null)
                throw new Exception();

            SerialPort.WriteBufferSize = 2560000;
            SerialPort.ReadBufferSize = 2560000;
            SerialPort.Open();
            //var getNodeIdsMessage = new BroadcastMessage { dest = 0, from = NodeId, type = PackageType.BROADCAST, msg = "ServerStarted" };
            //var getNodeIds = JsonConvert.SerializeObject(getNodeIdsMessage) + '\0';
            //SerialPort.Write(new byte[] {0,0,0,1 }, 0, 4);

            //NodeId = uint.Parse(SerialPort.ReadLine());

            StartListening();
            //SerialPort.DataReceived += SerialPort_DataReceived;
            return SerialPort;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            var line = SerialPort.ReadLine();
            if (line[0] != '{')
                return;
            JObject json;
            try
            {
                json = JsonConvert.DeserializeObject<JObject>(line);
            }
            catch (Exception)
            {
                return;
            }

            var bm = json.ToObject<BasicMessage>();

            if (!Connections.TryGetValue(bm.from, out var conn))
            {
                if (!json.ContainsKey("msg"))
                    return;
                BroadcastMessage broadcastMessage;
                try
                {
                    broadcastMessage = json.ToObject<BroadcastMessage>();
                }
                catch (Exception)
                {
                    return;
                }
                if (string.IsNullOrWhiteSpace(broadcastMessage.msg) || broadcastMessage.msg[0] != '{')
                {
                    var gsm = new GeneralSmarthomeMessage { MessageType = "Get", Command = "WhoAreYou" };
                    var sam = new SingleAdressedMessage() { dest = bm.from, from = NodeId, msg = JsonConvert.SerializeObject(gsm), type = PackageType.SINGLE };
                    var asd = JsonConvert.SerializeObject(sam) + '\0';

                    SerialPort.WriteLine(bm.from.ToString());
                    SerialPort.WriteLine(asd);
                    return;
                }

                SmarthomeCommand mt;
                try
                {
                    mt = JsonConvert.DeserializeObject<SmarthomeCommand>(broadcastMessage.msg);
                }
                catch (Exception)
                {

                    throw;
                }
                if (mt.Command == "WhoIAm")
                {
                    Connection meshConnection = new Connection()
                    {
                        NodeId = bm.from,
                        SerialPort = SerialPort
                    };
                    Connections[bm.from] = meshConnection;
                    NewConnectionEstablished?.Invoke(this, (meshConnection, mt.Parameters));
                }

            }

            if (Connections.TryGetValue(bm.from, out var con))
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

        public TcpListener ListenTCPAsync(ushort port)
        {
            var listener = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), port);
            listener.Start();
            listener.BeginAcceptTcpClient(InitConnection, null);
            return listener;
        }

        private void InitConnection(IAsyncResult ar) => StartListening();
        private void PrintToConsole(string s)
        {
            Console.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + ": " + s);
        }
        public void StartListening()
        {
            uint fromId = 0;

            new Task(() =>
            {
                try
                {
                    var ser = JsonSerializer.Create();

                    while (SerialPort != null && SerialPort.IsOpen)
                    //while (Client != null && Client.Connected == true)
                    {
                        if (SerialPort.BytesToRead == 0)
                        //if (Client.Available == 0)
                        {
                            if (!SerialPort.IsOpen)
                                //if (!Client.Connected)
                                break;
                            Thread.Sleep(1);
                            continue;
                        }


                        //using (var sr = new StreamReader(Client.GetStream(), Encoding.UTF8, false, 2048, true))
                        //{
                        //    using (var jr = new JsonTextReader(sr))
                        //    {
                        //        json = ser.Deserialize<JObject>(jr);
                        //    }
                        //} 
                        var line = SerialPort.ReadLine();
                        if (line[0] != '{')
                        {
                            Console.WriteLine(line + " || was not a valid JSON");
                            continue;

                        }
                        var json = JsonConvert.DeserializeObject<JObject>(line);

                        var bm = json.ToObject<BasicMessage>();

                        if (!Connections.TryGetValue(bm.from, out var conn))
                        {
                            if (!json.ContainsKey("msg"))
                                continue;
                            BroadcastMessage broadcastMessage;
                            try
                            {
                                broadcastMessage = json.ToObject<BroadcastMessage>();
                            }
                            catch (Exception)
                            {
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(broadcastMessage.msg) || broadcastMessage.msg[0] != '{')
                            {
                                var gsm = new GeneralSmarthomeMessage { MessageType = "Get", Command = "WhoAreYou" };
                                var sam = new SingleAdressedMessage() { dest = bm.from, from = NodeId, msg = JsonConvert.SerializeObject(gsm), type = PackageType.SINGLE };
                                var asd = JsonConvert.SerializeObject(sam) + '\0';

                                SerialPort.WriteLine(bm.from.ToString());
                                SerialPort.WriteLine(asd);
                                continue;
                            }
                            var mt = JsonConvert.DeserializeObject<SmarthomeCommand>(broadcastMessage.msg);
                            if (mt.Command == "WhoIAm")
                            {
                                fromId = bm.from;
                                Connection meshConnection = new Connection()
                                {
                                    NodeId = fromId,
                                    //Client = Client
                                    SerialPort = SerialPort
                                };
                                Connections[fromId] = meshConnection;
                                NewConnectionEstablished?.Invoke(this, (meshConnection, mt.Parameters));
                                //SendSingle(fromId, "");
                            }
                            //}

                        }

                        if (Connections.TryGetValue(fromId, out var con))
                        {
                            switch (bm.type)
                            {
                                case PackageType.DROP:
                                    break;
                                case PackageType.TIME_SYNC:
                                    //HandleMessage(json.ToObject<TimeSync>());
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
                    //Client = null;
                    if (SerialPort != null)
                    {
                        SerialPort.Close();
                        SerialPort = null;
                    }
                }
                catch (Exception e)
                {
                    PrintToConsole(e.Message);
                    Connections.Remove(fromId);
                    //Client = null;
                    if (SerialPort != null)
                    {
                        SerialPort.Close();
                        SerialPort = null;
                    }
                }
            }, TaskCreationOptions.LongRunning
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
