using AppBrokerASP.Configuration;
using AppBrokerASP.Devices.Zigbee;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using AppBrokerASP.Extension;
using SocketIOClient;
using AppBroker.Core.Devices;
using System.Text.RegularExpressions;
using NonSucking.Framework.Extension.Threading;
using AppBroker.Core.Database;
using AppBroker.Core;

namespace AppBrokerASP.IOBroker;

public class IoBrokerManager
{
    public event EventHandler<(long id, Device device)>? NewDeviceAdded;

    private SocketIO? client;
    private readonly NLog.Logger logger;
    private readonly ZigbeeConfig Config;
    private readonly HttpClient http;

    public IoBrokerManager(NLog.Logger logger, HttpClient http, ConcurrentDictionary<long, Device> devices, ZigbeeConfig config)
    {
        this.logger = logger;
        this.http = http;
        Config = config;
    }

    public async Task ConnectToIOBroker()
    {
        client = new SocketIO(new Uri(Config.SocketIOUrl), new SocketIOOptions()
        {
            EIO = Config.NewSocketIoversion ? EngineIO.V4 : EngineIO.V3
        });

        ScopedSemaphore stateSemaphroe = new();
        client.OnAny(async (eventName, response) =>
        {
            if (IoBrokerZigbee.TryParse(eventName, response.ToString(), out var zo) && zo is not null)
            {
                using var _ = stateSemaphroe.Wait();
                if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(zo.Id, out var dev) && dev is ZigbeeDevice zigbeeDev)
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

                using var _ = stateSemaphroe.Wait();
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
        var existingDevices = IInstanceContainer.Instance.DeviceManager.Devices;
        List<Device> devices = new List<Device>();
        foreach (var deviceRes in getDeviceResponses)
        {
            if (!long.TryParse(deviceRes.native.id, System.Globalization.NumberStyles.HexNumber, null, out var id))
                continue;
            if (!existingDevices.TryGetValue(id, out var dev))
            {
                dev = devices.FirstOrDefault(x => x.Id == id);
                if (dev is null)
                {
                    var deviceName = deviceRes.common.type;
                    dev = IInstanceContainer.Instance.DeviceTypeMetaDataManager.CreateDeviceFromName(deviceName, typeof(UpdateableZigbeeDevice), new object[] { id, client }, new object[] { id, client, deviceName });

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

                    devices.Add(dev);
                }
            }

            foreach (var item in deviceStates.Where(x => x._id.Contains(deviceRes.native.id)))
            {
                var ioObject = new IoBrokerObject(BrokerEvent.StateChange, "", 0, item._id[(item._id.LastIndexOf(".") + 1)..], new Parameter(item.val));
                if (ioObject.ValueName == "msg_from_zigbee" || ioObject.ValueName == "device_query")
                    continue;

                if (dev is ZigbeeDevice zd)
                    zd.SetPropFromIoBroker(ioObject, false);
            }
            dev.Initialized = true;
        }
        if (devices.Count > 0)
            IInstanceContainer.Instance.DeviceManager.AddNewDevices(devices);
    }

}
