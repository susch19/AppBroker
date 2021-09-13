using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using AppBrokerASP.Database;
using AppBrokerASP.Devices;
using AppBrokerASP.Devices.Painless;
using AppBrokerASP.Devices.Zigbee;
using AppBrokerASP.IOBroker;

using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

using PainlessMesh;
using Microsoft.Extensions.Configuration;
using AppBrokerASP.Configuration;
using System.Net.Http;
using SocketIOClient;

namespace AppBrokerASP
{
    public class DeviceManager
    {
        public ZigbeeConfig Config { get; }
        public ConcurrentDictionary<long, Device> Devices = new();
        private SocketIO? client;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<Type> types;
        readonly Task temp;
        private static readonly HttpClient http = new();

        public DeviceManager()
        {
            Config = InstanceContainer.ConfigManager.ZigbeeConfig;
 
            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();
            InstanceContainer.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
            InstanceContainer.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
            InstanceContainer.MeshManager.ConnectionReastablished += MeshManager_ConnectionReastablished;
            temp = ConnectToIOBroker();
        }

        private void MeshManager_ConnectionLost(object? sender, uint e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.StopDevice();
            }
        }
        private void MeshManager_ConnectionReastablished(object? sender, (uint id, ByteLengthList parameter) e)
        {
            if (Devices.TryGetValue(e.id, out var device))
            {
                //while (!Devices.TryRemove(e, out var d)) { }
                //Console.WriteLine($"Removed device {device.Id} - {device.FriendlyName} - {device.GetType()}");
                device.Reconnect(e.parameter);
            }
        }

        //private void DeviceReconnect((Sub c, List<string> l) e, List<Subscriber> clients)
        //{
        //    var newDevice = (Device)Activator.CreateInstance(types.FirstOrDefault(x => x.Name.ToLower() == e.l[1].ToLower()), e.c.NodeId);
        //    newDevice.Subscribers = clients;
        //    logger.Info($"Device reconnected: {newDevice.TypeName}, {newDevice.Id} | Subscribers: {newDevice.Subscribers.Count}");
        //    while (!Devices.TryAdd(e.c.NodeId, newDevice)) { }
        //}

        private void Node_NewConnectionEstablished(object? sender, (Sub c, ByteLengthList l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                device.Reconnect(e.l);
            }
            else
            {
                var deviceName = Encoding.UTF8.GetString(e.l[1]);
                logger.Debug($"Trying to get device with {deviceName} name");
                var type = types.FirstOrDefault(x =>
                       x.Name.Equals(deviceName, StringComparison.InvariantCultureIgnoreCase)
                           || x.GetCustomAttribute<PainlessMeshNameAttribute>()?.AlternateName == deviceName);

                if (type is null)
                {
                    logger.Error($"Failed to get device with {deviceName} name");
                    return;
                }

                var newDeviceObj = Activator.CreateInstance(type, e.c.NodeId, e.l);

                if (newDeviceObj is null || !(newDeviceObj is Device newDevice))
                    return;
                if (newDevice is null)
                {
                    logger.Error($"Failed to get create device {deviceName}");
                    return;
                }
                logger.Debug($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                _ = Devices.TryAdd(e.c.NodeId, newDevice);
                if (!DbProvider.AddDeviceToDb(newDevice))
                    _ = DbProvider.MergeDeviceWithDbData(newDevice);
            }
        }

        private async Task ConnectToIOBroker()
        {

            client = new SocketIO(new Uri(Config.SocketIOUrl), new SocketIOOptions()
            {
                EIO = Config.NewSocketIoversion ? 4 : 3
            });
            //int i = 0;
            client.OnAny(async (eventName, response) =>
                {
                    var suc = IoBrokerZigbee.TryParse(eventName, response.ToString(), out var zo);
                    //Console.Write(i++ + ", ");
                    if (suc && zo is not null)
                    {

                        if (Devices.TryGetValue(zo.Id, out var dev) && dev is ZigbeeDevice zigbeeDev)
                        {
                            try
                            {
                                zigbeeDev.SetPropFromIoBroker(zo, true);
                            }
                            catch (Exception e)
                            {
                                logger.Error(e);
                            }
                        }
                        else
                            await GetZigbeeDevices();
                    }
                });
            client.OnError += async (sender, args) =>
            {
                logger.Error($"AfterException, trying reconnect: {args}");
                await client.DisconnectAsync();
                await client.ConnectAsync();
            };

            client.OnConnected += async (s, e) =>
            {
                Console.WriteLine("Connected");
                logger.Debug("Connected Zigbee Client");
                await client.EmitAsync("subscribe", "zigbee.*");
                await client.EmitAsync("subscribeObjects", '*');
                await GetZigbeeDevices();
            };
            var random = new Random();
            await client.ConnectAsync();
            //await client.Emit("setState", new { id = "zigbee.0.d0cf5efffe1fa105.colortemp", val = 400, ack = true, ts = DateTime.Now.Ticks });
        }

        public async Task GetZigbeeDevices()
        {
            
            if (!Devices.IsEmpty)
            {
                logger.Debug($"Cancel {nameof(GetZigbeeDevices)}, because it wasn't the startup");
                return;
            }

            string content = await http.GetStringAsync(@$"{Config.HttpUrl}/objects?pattern=zigbee.0*");
            var better = Regex.Replace(content, "\"zigbee[.\\w\\s\\d]+\":", "");
            better = $"[{better[1..^1]}]";

            var ioBrokerObject = JsonConvert.DeserializeObject<ZigbeeIOBrokerProperty[]>(better);
            if (ioBrokerObject is null)
            {
                logger.Error($"Error deserializing IOBroker Property");

                return;
            }
            var idRequest = @$"{Config.HttpUrl}/get/";
            var idsAlreadyInRequest = new List<ulong>();
            var stateRequest = @$"{Config.HttpUrl}/get/";
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

            content = await http.GetStringAsync(idRequest);


            var getDeviceResponses = JsonConvert.DeserializeObject<IoBrokerGetDeviceResponse[]>(content)!;
            content = await http.GetStringAsync(stateRequest);
            var deviceStates = JsonConvert.DeserializeObject<IoBrokerStateResponse[]>(content)!;

            foreach (var deviceRes in getDeviceResponses)
            {
                if (!long.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
                    continue;

                if (!Devices.TryGetValue(id, out var dev))
                {
                    switch (deviceRes.common.type)
                    {
                        case "WSDCGQ11LM":
                        case "lumi.weather": dev = new XiaomiTempSensor(id, client!); break;
                        case "lumi.router": dev = new LumiRouter(id, client!); break;
                        case "L1529":
                        case "FLOALT panel WS 60x60": dev = new FloaltPanel(id, @$"{Config.HttpUrl}/set/" + deviceRes._id, client!); break;
                        case "E1524/E1810":

                        case "TRADFRI remote control": dev = new TradfriRemoteControl(id, client!); break;
                        case "AB32840":
                        case "Classic B40 TW - LIGHTIFY": dev = new OsramB40RW(id, @$"{Config.HttpUrl}/set/" + deviceRes._id, client!); break;
                        case "AB3257001NJ":
                        case "Plug 01": dev = new OsramPlug(id, @$"{Config.HttpUrl}/set/" + deviceRes._id, client!); break;
                        case "LED1624G9":
                        case "TRADFRI bulb E14 CWS opal 600lm":
                        case "TRADFRI bulb E27 CWS opal 600lm":
                            dev = new TradfriLedBulb(id, "", client!); break;
                        default:
                            logger.Warn($"Found not mapped device: {deviceRes.common.name} ({deviceRes.common.type})");
                            break;
                    }
                    if (dev == default(Device))
                        continue;
                    if (dev is ZigbeeDevice zd)
                        zd.AdapterWithId = deviceRes._id;
                    _ = Devices.TryAdd(id, dev);
                    if (!DbProvider.AddDeviceToDb(dev))
                        _ = DbProvider.MergeDeviceWithDbData(dev);

                    // If name is only numbers, try to get better name
                    if (string.IsNullOrWhiteSpace(dev.FriendlyName) || long.TryParse(dev.FriendlyName, out _))
                        dev.FriendlyName = deviceRes.common.name;
                }
                foreach (var item in deviceStates.Where(x => x._id.Contains(deviceRes.native.id)))
                {
                    var ioObject = new IoBrokerObject(BrokerEvent.StateChange, "", 0, item.common.name.ToLower().Replace(" ", "_"), new Parameter(item.val));
                    if (dev is XiaomiTempSensor)
                    {
                        if (ioObject.ValueName == "battery_voltage")
                            ioObject.ValueName = "voltage";
                        else if (ioObject.ValueName == "battery_percent")
                            ioObject.ValueName = "battery";
                    }
                    else if (dev is FloaltPanel)
                    {

                        if (ioObject.ValueName == "color_temperature")
                            ioObject.ValueName = "colortemp";
                        else if (ioObject.ValueName == "switch_state")
                            ioObject.ValueName = "state";
                    }
                    if (dev is ZigbeeDevice zd)
                        zd.SetPropFromIoBroker(ioObject, false);
                }
            }
        }
    }
}
