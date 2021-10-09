using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AppBrokerASP.Database;
using AppBrokerASP.Devices;
using AppBrokerASP.Devices.Zigbee;
using AppBrokerASP.IOBroker;
using Newtonsoft.Json;

using PainlessMesh;
using Microsoft.Extensions.Configuration;
using AppBrokerASP.Configuration;
using System.Net.Http;
using SocketIOClient;
using AppBrokerASP.Extension;
using Newtonsoft.Json.Linq;

namespace AppBrokerASP
{
    public class DeviceManager
    {
        public ZigbeeConfig Config { get; }
        public ConcurrentDictionary<long, Device> Devices = new();
        private SocketIO? client;
        private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly List<Type> types;
        private readonly Dictionary<string, Type> alternativeNamesForTypes = new(StringComparer.OrdinalIgnoreCase);
        readonly Task temp;
        private static readonly HttpClient http = new();

        public DeviceManager()
        {
            Config = InstanceContainer.ConfigManager.ZigbeeConfig;

            types = Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device)).ToList();

            foreach (var type in types)
            {
                var names = GetAllNamesFor(type);
                foreach (var name in names)
                {
                    alternativeNamesForTypes[name] = type;
                }
            }

            InstanceContainer.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
            InstanceContainer.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
            InstanceContainer.MeshManager.ConnectionReastablished += MeshManager_ConnectionReastablished;

            temp = ConnectToIOBroker();
        }

        private List<string> GetAllNamesFor(Type y)
        {
            var names = new List<string>();
            var attribute = y.GetCustomAttribute<DeviceNameAttribute>();

            if (attribute is not null)
            {
                names.Add(attribute.PreferredName);
                names.AddRange(attribute.AlternativeNames);
            }
            names.Add(y.Name);
            return names;
        }

        private void MeshManager_ConnectionLost(object? sender, uint e)
        {
            if (Devices.TryGetValue(e, out var device))
            {
                device.StopDevice();
            }
        }
        private void MeshManager_ConnectionReastablished(object? sender, (uint id, ByteLengthList parameter) e)
        {
            if (Devices.TryGetValue(e.id, out var device))
            {
                device.Reconnect(e.parameter);
            }
        }

        private void Node_NewConnectionEstablished(object? sender, (Sub c, ByteLengthList l) e)
        {
            if (Devices.TryGetValue(e.c.NodeId, out var device))
            {
                device.Reconnect(e.l);
            }
            else
            {
                var deviceName = Encoding.UTF8.GetString(e.l[1]);
                var newDevice = CreateDeviceFromName(deviceName, e.c.NodeId, e.l);

                if (newDevice is null)
                    return;

                logger.Debug($"New Device: {newDevice.TypeName}, {newDevice.Id}");
                _ = Devices.TryAdd(e.c.NodeId, newDevice);

                if (!DbProvider.AddDeviceToDb(newDevice))
                    _ = DbProvider.MergeDeviceWithDbData(newDevice);
            }
        }

        private Device? CreateDeviceFromName(string deviceName, params object[] ctorArgs)
        {
            logger.Trace($"Trying to get device with {deviceName} name");

            if (!alternativeNamesForTypes.TryGetValue(deviceName, out var type) || type is null)
            {
                logger.Error($"Failed to get device with name {deviceName}");
                return null;
            }

            var newDeviceObj = Activator.CreateInstance(type, ctorArgs);

            if (newDeviceObj is null || newDeviceObj is not Device newDevice)
            {
                logger.Error($"Failed to get create device {deviceName}");
                return null;
            }

            return newDevice;
        }

        private async Task ConnectToIOBroker()
        {
            client = new SocketIO(new Uri(Config.SocketIOUrl), new SocketIOOptions()
            {
                EIO = Config.NewSocketIoversion ? 4 : 3
            });

            client.OnAny(async (eventName, response) =>
            {
                if (IoBrokerZigbee.TryParse(eventName, response.ToString(), out var zo) && zo is not null)
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
                    {
                        try
                        {

                            await GetZigbeeDevices(client);
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                        }
                    }
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
                logger.Debug("Connected Zigbee Client");
                await client.EmitAsync("subscribe", "zigbee.*");
                await client.EmitAsync("subscribeObjects", '*');
                try
                {

                    await GetZigbeeDevices(client);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            };

            await client.ConnectAsync();
        }

        public async Task GetZigbeeDevices(SocketIO socket)
        {
            if (!Devices.IsEmpty)
            {
                logger.Debug($"Cancel {nameof(GetZigbeeDevices)}, because it wasn't the startup");
                return;
            }

            if (client is null)
            {
                logger.Error("SocketIO Client is null");
                return;
            }

            var allObjectsResponse = await socket.Emit("getObjects");
            var allObjectscontentNew = allObjectsResponse?.GetValue(1).ToString();

            if (allObjectscontentNew is null)
            {
                logger.Error("getObjects response is empty");
                return;
            }

            var ioBrokerObject = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(allObjectscontentNew)?
                .Where(x => x.Key.StartsWith("zigbee.0."))!
                .ToDictionary(x => x.Key, x => x.Value.ToObject<ZigbeeIOBrokerProperty>())
                .Values.AsEnumerable();

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
                if (item is null)
                    continue;

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

            var content = await http.GetStringAsync(idRequest);

            var getDeviceResponses = JsonConvert.DeserializeObject<IoBrokerGetDeviceResponse[]>(content)!;
            content = await http.GetStringAsync(stateRequest);
            var deviceStates = JsonConvert.DeserializeObject<IoBrokerStateResponse[]>(content)!;

            foreach (var deviceRes in getDeviceResponses)
            {
                if (!long.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
                    continue;

                if (!Devices.TryGetValue(id, out var dev))
                {
                    dev = CreateDeviceFromName(deviceRes.common.type, id, client);

                    if (dev is null or default(Device))
                    {
                        logger.Warn($"Found not mapped device: {deviceRes.common.name} ({deviceRes.common.type})");
                        continue;
                    }

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
