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
using AppBroker.Core;
using AppBroker.Core.Devices;

namespace AppBrokerASP;

public class DeviceManager : IDisposable, IDeviceManager
{
    public ZigbeeConfig Config { get; }
    public ConcurrentDictionary<long, Device> Devices { get; } = new();
    public IReadOnlyCollection<Type> DeviceTypes => types;

    public event EventHandler<(long id, Device device)>? NewDeviceAdded;

    private SocketIO? client;
    private bool disposed;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly List<Type> types;
    private readonly Dictionary<string, Type> alternativeNamesForTypes = new(StringComparer.OrdinalIgnoreCase);
    readonly Task temp;
    private static readonly HttpClient http = new();

    public DeviceManager()
    {
        Config = InstanceContainer.Instance.ConfigManager.ZigbeeConfig;

        types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device))
            .ToList();

        foreach (var type in types)
        {
            var names = GetAllNamesFor(type);
            foreach (var name in names)
            {
                alternativeNamesForTypes[name] = type;
            }
        }

        InstanceContainer.Instance.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
        InstanceContainer.Instance.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
        InstanceContainer.Instance.MeshManager.ConnectionReastablished += MeshManager_ConnectionReastablished;

        temp = ConnectToIOBroker();
    }
    private void AddNewDeviceToDic(Device device)
    {
        if (Devices.TryAdd(device.Id, device))
            NewDeviceAdded?.Invoke(this, (device.Id, device));
    }

    private static List<string> GetAllNamesFor(Type y)
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
            AddNewDeviceToDic( newDevice);
            //_ = Devices.TryAdd(e.c.NodeId, newDevice);

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

        const int maxUrlLength = 1900;

        var baseStateRequest = @$"{Config.HttpUrl}/get/";
        var idRequest = @$"{Config.HttpUrl}/get/";

        var idsAlreadyInRequest = new List<ulong>();
        var requests = new List<string>();

        var currentStateRequest = baseStateRequest;

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
                    var addItem = item._id + ",";

                    if (currentStateRequest.Length + addItem.Length > maxUrlLength)
                    {
                        requests.Add(currentStateRequest);
                        currentStateRequest = baseStateRequest;
                    }

                    currentStateRequest += addItem;
                }
            }
        }

        if (currentStateRequest != baseStateRequest)
            requests.Add(currentStateRequest);

        var content = await http.GetStringAsync(idRequest);

        var getDeviceResponses = JsonConvert.DeserializeObject<IoBrokerGetDeviceResponse[]>(content)!;

        var deviceStates = new List<IoBrokerStateResponse>();

        foreach (var stateRequest in requests)
        {
            content = await http.GetStringAsync(stateRequest);
            deviceStates.AddRange(JsonConvert.DeserializeObject<IoBrokerStateResponse[]>(content)!);
        }

        foreach (var deviceRes in getDeviceResponses)
        {
            if (deviceRes.native is null
                || deviceRes.native.id is null
                || !long.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
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

                //_ = Devices.TryAdd(id, dev);

                if (!DbProvider.AddDeviceToDb(dev))
                    _ = DbProvider.MergeDeviceWithDbData(dev);

                // If name is only numbers, try to get better name
                if (string.IsNullOrWhiteSpace(dev.FriendlyName) || long.TryParse(dev.FriendlyName, out _))
                    dev.FriendlyName = deviceRes.common.name;

                AddNewDeviceToDic(dev);
            }

            foreach (var item in deviceStates.Where(x => !string.IsNullOrWhiteSpace(x._id) && x._id.Contains(deviceRes.native.id)))
            {
                var itemId = item._id[(item._id.LastIndexOf(".") + 1)..].ToLower();

                var ioObject = new IoBrokerObject(BrokerEvent.StateChange, "", 0, itemId, new Parameter(item.val));
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
            dev.Initialized = true;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                foreach (var device in Devices.Values.OfType<IDisposable>())
                {
                    device?.Dispose();
                }
                Devices.Clear();
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}