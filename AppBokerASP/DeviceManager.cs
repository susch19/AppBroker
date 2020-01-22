using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Devices;
using AppBokerASP.Devices.Zigbee;
using AppBokerASP.IOBroker;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using PainlessMesh;
using SimpleSocketIoClient;

namespace AppBokerASP
{
    public class DeviceManager
    {
        public ConcurrentDictionary<ulong, Device> Devices = new ConcurrentDictionary<ulong, Device>();
        private SocketIoClient client;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private List<Type> types;
        Task temp;
        public DeviceManager()
        {
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            Program.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
            Program.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
            temp = ConnectToIOBroker();
        }

        private void MeshManager_ConnectionLost(object sender, uint e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                device.IsConnected = false;
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.StopDevice();
            }
        }

        private void DeviceReconnect((Sub c, List<string> l) e, List<Subscriber> clients)
        {
            var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
            newDevice.Subscribers = clients;
            logger.Info($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
            while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
        }

        private void Node_NewConnectionEstablished(object sender, (Sub c, List<string> l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                device.IsConnected = true;
                device.Reconnect();
                //while (!Devices.TryRemove(e.c.NodeId, out var d)) { }
                //device.StopDevice();
                //DeviceReconnect(e, device.Subscribers);
                //device = null;
            }
            else
            {

                var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
                logger.Info($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
                if (!DbProvider.AddDeviceToDb(newDevice))
                    DbProvider.MergeDeviceWithDbData(newDevice);
                //using var cont = DbProvider.BrokerDbContext;
                //if(!cont.Devices.Any(x => x.Id == e.c.NodeId))
                //{
                //    cont.Devices.Add(newDevice);
                //    cont.SaveChanges();
                //}
            }
        }

        private async Task ConnectToIOBroker()
        {

            client = new SocketIoClient();

            client.AfterEvent += (sender, args) =>
                {
                    var suc = IoBrokerZigbee.TryParse(args.Value, out var zo);
                    if (suc)
                    {
                        if (Devices.TryGetValue(zo.Id, out var dev))
                            (dev as ZigbeeDevice).SetPropFromIoBroker(zo, true);
                    }
                };
            client.AfterException += (sender, args) => logger.Error($"AfterException: {args.Value}");
            client.Connected += (s, e) =>
            {
                Console.WriteLine("Connected");
                client.Emit("subscribe", '*');
                client.Emit("subscribeObjects", '*');
                GetZigbeeDevices();
            };
            var random = new Random();
            await client.ConnectAsync(new Uri("http://ZigbeeHub:8084"));
            await client.Emit("setState", new { id = "zigbee.0.d0cf5efffe1fa105.colortemp", val = 400, ack = true, ts = DateTime.Now.Ticks });
        }

        public void GetZigbeeDevices()
        {

            string content = RequestStringData(@"http://ZigbeeHub:8087/objects?pattern=zigbee.0*");
            var better = Regex.Replace(content, "\"zigbee[.\\w\\s\\d]+\":", "");
            better = $"[{better[1..^1]}]";

            var ioBrokerObject = JsonConvert.DeserializeObject<ZigbeeIOBrokerProperty[]>(better);
            var idRequest = "http://ZigbeeHub:8087/get/";
            var idsAlreadyInRequest = new List<ulong>();
            var stateRequest = "http://ZigbeeHub:8087/get/";
            foreach (var item in ioBrokerObject)
            {
                var matches = item._id.Split('.');
                if (matches.Length > 2)
                {
                    if (ulong.TryParse(matches[2].ToString(), System.Globalization.NumberStyles.HexNumber, null, out var id))
                    {
                        if (!idsAlreadyInRequest.Contains(id))
                        {
                            idRequest += string.Join('.', matches.Take(3)) + ",";
                            idsAlreadyInRequest.Add(id);
                        }
                        stateRequest += item._id + ",";
                    }
                }
            }

            content = RequestStringData(idRequest);


            var getDeviceResponses = JsonConvert.DeserializeObject<IoBrokerGetDeviceResponse[]>(content);
            content = RequestStringData(stateRequest);
            var deviceStates = JsonConvert.DeserializeObject<IoBrokerStateResponse[]>(content);

            foreach (var deviceRes in getDeviceResponses)
            {
                if (!ulong.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
                    continue;

                if (!Devices.TryGetValue(id, out var dev))
                {
                    switch (deviceRes.common.type)
                    {
                        case "lumi.weather": dev = new XiaomiTempSensor(id); break;
                        case "lumi.router": break;
                        case "FLOALT panel WS 60x60": dev = new FloaltPanel(id, "http://ZigbeeHub:8087/set/"+deviceRes._id); break;
                        case "TRADFRI remote control": break;
                        default: break;
                    }
                    if (dev == default(Device))
                        continue;
                    Devices.TryAdd(id, dev);
                    if (!DbProvider.AddDeviceToDb(dev))
                        DbProvider.MergeDeviceWithDbData(dev);
                    if (string.IsNullOrWhiteSpace(dev.FriendlyName))
                        dev.FriendlyName = deviceRes.common.name;
                }
                foreach (var item in deviceStates.Where(x => x._id.Contains(deviceRes.native.id)))
                {
                    var ioObject = new IoBrokerObject { ValueName = item.common.name.ToLower().Replace(" ", "_"), ValueParameter = new Parameter { Value = item.val} };
                    (dev as ZigbeeDevice).SetPropFromIoBroker(ioObject, false);
                }
            }
        }

        private string RequestStringData(string url)
        {
            var request = WebRequest.CreateHttp(url);
            var res = request.GetResponse();
            using var stream = res.GetResponseStream();
            using var streamreader = new StreamReader(stream);
            return streamreader.ReadToEnd();
        }
    }
}
