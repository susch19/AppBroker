using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Newtonsoft.Json;
using NLog;

using Newtonsoft.Json.Linq;
using System.Globalization;
using AppBroker.Core;
using AppBroker.Core.Database;
using AppBrokerASP;

using AppBroker.Zigbee2Mqtt.Devices;
using System.Diagnostics.CodeAnalysis;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;

using ZigbeeConfig = AppBroker.Zigbee2Mqtt.Zigbee2MqttConfig;
using Quartz.Util;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AppBroker.Zigbee2Mqtt;

public class Zigbee2MqttManager : IAsyncDisposable
{
    public IManagedMqttClient? MQTTClient { get; set; }
    internal readonly Dictionary<string, long> friendlyNameToIdMapping = new();
    private Zigbee2MqttDeviceJson[]? devices;
    private readonly ZigbeeConfig config;
    private readonly Logger logger;
    private readonly Stack<(string, string)> cachedBeforeConnect = new();

    public Zigbee2MqttManager(ZigbeeConfig zigbee2MqttConfig)
    {
        config = zigbee2MqttConfig;
        logger = LogManager.GetCurrentClassLogger();

        IInstanceContainer.Instance.JavaScriptEngineManager.ExtendedFunctions["setValueZigbeeId"] = new Func<long, string, JToken, Task<bool>>(SetValue);
        IInstanceContainer.Instance.JavaScriptEngineManager.ExtendedFunctions["setValueZigbeeName"] = new Func<string, string, JToken, Task<bool>>(SetValue);
        IInstanceContainer.Instance.JavaScriptEngineManager.ExtendedFunctions["sendToZigbee"] = EnqueueToZigbee;
    }

    public async Task Subscribe()
    {
        if (MQTTClient is null)
            return;

        try
        {
            logger.Debug("Subscribing to zigbee2mqtt topic");
            await MQTTClient.SubscribeAsync($"{config.Topic}/#");

        }
        catch (Exception ex)
        {
            logger.Error("Erorr during subscribing to zigbee2mqtt topic", ex);
        }
    }

    public Task SetOption(string name, string propName, JToken value)
    {
        return MQTTClient.EnqueueAsync($"{config.Topic}/bridge/request/device/options", $$"""{"id":{{name}}, "options":{"{{propName}}":{{value}}} }""");
    }

    public Task SetCommand(long deviceId, ushort cluster, byte command, JToken payload)
    {
        var body =
            $$"""
            {
                "command": {
                    "cluster": {{cluster}},
                    "command": {{command}},
                    "payload": {{payload.ToString()}}
                }
            }
            """;
        var hexId = deviceId.ToString("x2");
        return MQTTClient.EnqueueAsync($"{config.Topic}/0x{hexId}/set", body);
    }

    public Task<bool> SetValue(string deviceName, string propertyName, JToken newValue)
    {
        if (MQTTClient is null)
            return Task.FromResult(false);
        var deviceId = IInstanceContainer.Instance.DeviceManager.Devices.FirstOrDefault(x => x.Value.FriendlyName == deviceName).Key;
        if (deviceId == default)
            return Task.FromResult(false);

        return SetValue(deviceId, propertyName, newValue);

    }

    public async Task<bool> SetValue(long deviceId, string propertyName, JToken newValue)
    {
        if (MQTTClient is null)
            return false;
        var hexId = deviceId.ToString("x2");

        logger.Info($"Updating device 0x{hexId} state {propertyName} with new value {newValue}");

        string val = newValue.Type switch
        {
            JTokenType.Float => newValue.Value<float>().ToString(CultureInfo.InvariantCulture),
            _ => newValue.ToString(),
        };

        await MQTTClient.EnqueueAsync($"{config.Topic}/0x{hexId}/set/{propertyName}", val);
        return true;
    }
    public Task<bool> SetValue(Device device, string propertyName, JToken newValue)
    {
        return SetValue(device.Id, propertyName, newValue);
    }

    public Task EnqueueToZigbee(string path, JToken payload)
    {
        return MQTTClient.EnqueueAsync($"{config.Topic}/{path}", payload.ToString());
    }

    public async Task<IManagedMqttClient> Connect()
    {
        logger.Debug("Connecting to mqtt");
        if (MQTTClient is not null)
        {
            logger.Debug("Already connected to mqtt, returing existing instance");
            return MQTTClient;
        }

        var mqttFactory = new MqttFactory();
        var managedMqttClient = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer(config.Address, config.Port)
                        .Build();
        logger.Debug("Builded new mqtt tcp server options");

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();
        logger.Debug("Builded new mqtt tcp client options");

        MQTTClient = managedMqttClient;
        MQTTClient.ApplicationMessageReceivedAsync += async (e) =>
        {

            try
            {
                await Mqtt_ApplicationMessageReceivedAsync(e);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                logger.Error($"{e.ApplicationMessage.Topic}{Environment.NewLine}{e.ApplicationMessage.ConvertPayloadToString()}");

            }
        };
        logger.Debug("Subscribed the incomming mqtt messages");
        await managedMqttClient.StartAsync(managedMqttClientOptions);
        logger.Debug("Started the mqtt client");
        return MQTTClient;
    }

    private async Task Mqtt_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (!topic.StartsWith($"{config.Topic}/"))
        {
            return;
        }

        topic = topic[(config.Topic.Length + 1)..];
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        if (topic == "bridge/info")
        {
            var c = JsonConvert.DeserializeObject<Zigbee2MqttBridgeInfo>(payload);
            var manager = IInstanceContainer.Instance.DeviceStateManager;
            if (!long.TryParse(c.Coordinator.IEEEAddress, NumberStyles.HexNumber, null,  out var coordinatorId))
                return;
            manager.SetSingleState(coordinatorId, nameof(c.PermitJoin), c.PermitJoin);
            manager.SetSingleState(coordinatorId, nameof(c.PermitJoinTimeout), c.PermitJoinTimeout);
            manager.SetSingleState(coordinatorId, nameof(c.RestartRequired), c.RestartRequired);
        }
        else if (topic == "bridge/state")
        {

        }
        else if (topic == "bridge/logging")
        {
            var g = JsonConvert.DeserializeObject<Zigbee2MqttLogMessage>(payload);
            var lvl = g!.Level switch
            {
                Zigbee2MqttLogLevel.Error => LogLevel.Error,
                Zigbee2MqttLogLevel.Warning => LogLevel.Warn,
                Zigbee2MqttLogLevel.Info => LogLevel.Info,
                _ => LogLevel.Info,
            };
            _ = logger.ForLogEvent(lvl).Message(g.Message).Callsite();
        }
        else if (topic == "bridge/devices")
        {
            try
            {

                devices = JsonConvert.DeserializeObject<Zigbee2MqttDeviceJson[]>(payload);
            }
            catch (Exception)
            {

                throw;
            }
            using (var ctx = DbProvider.BrokerDbContext)
            {
                foreach (var item in devices!)
                {
                    var id = long.Parse(item.IEEEAddress[2..], NumberStyles.HexNumber);
                    logger.Debug($"Trying to create new device {id}");
                    var dbDevice = ctx.Devices.FirstOrDefault(x => x.Id == id);
                    if (dbDevice is not null && !string.IsNullOrWhiteSpace(dbDevice.FriendlyName) && !string.Equals(dbDevice.FriendlyName, item.FriendlyName, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.Info($"Friendly name of Zigbee2Mqtt Device {item.FriendlyName} does not match saved name {dbDevice.FriendlyName}, updating");
                        await MQTTClient.EnqueueAsync($"{config.Topic}/bridge/request/device/rename", $"{{\"from\": \"{item.IEEEAddress}\", \"to\": \"{dbDevice.FriendlyName}\"}}");
                        item.FriendlyName = dbDevice.FriendlyName;
                    }

                    friendlyNameToIdMapping[item.FriendlyName] = id;
                    if (IInstanceContainer.Instance.DeviceManager.Devices.ContainsKey(id))
                    {
                        logger.Debug($"Already having device {id} ");
                        continue;
                    }

                    Device? dev;

                    if (item.Type == Zigbee2MqttDeviceType.Coordinator)
                        dev = new CoordinatorDevice(item, id);
                    else
                        dev = IInstanceContainer.Instance.DeviceTypeMetaDataManager.CreateDeviceFromNameWithBaseType(Zigbee2MqttDevice.GetTypeName(item), typeof(Zigbee2MqttDevice), typeof(Zigbee2MqttDevice), item!, id);

                    if (dev is not null)
                    {
                        logger.Info($"Got new device {item.FriendlyName} with id {id}");
                        InstanceContainer.Instance.DeviceManager.AddNewDevice(dev);
                    }
                    else
                    {
                        logger.Info($"Couldn't initialize device {item.FriendlyName} with id {id}");
                    }
                }
            }

            while (cachedBeforeConnect.TryPop(out var item))
            {
                logger.Debug($"Popped state for {item.Item1}:{item.Item2}, {cachedBeforeConnect.Count} left to go");
                TryInterpretTopicAsStateUpdate(item.Item1, item.Item2);
            }
        }
        else if (topic == "bridge/groups")
        {
            //var d = JsonConvert.DeserializeObject<Zigbee2MqttGroup[]>(payload);
        }
        else if (topic == "bridge/event")
        {

        }
        else if (topic == "bridge/extensions")
        {

        }
        else if (topic == "bridge/definitions")
        {

        }
        else if (topic.EndsWith("/availability", StringComparison.OrdinalIgnoreCase))
        {
            var deviceName = topic[0..^"/availability".Length];

            if (friendlyNameToIdMapping.TryGetValue(deviceName, out var deviceId))
            {
                InstanceContainer
                    .Instance
                    .DeviceStateManager
                    .SetSingleState(deviceId, "available", payload == "online" || payload == "{\"state\":\"online\"}");
            }
            else
            {
                logger.Warn($"Couldn't set availability ({payload}) on {deviceName}");
            }
        }
        else if (topic == "bridge/converters")
        {

        }
        else
        {
            logger.Trace($"[{topic}] {payload}");
            if (devices is null)
            {
                logger.Trace($"Got state before device {topic}, is something wrong with the retained messages of the mqtt broker?");
                cachedBeforeConnect.Push((topic, payload));
                return;
            }
            TryInterpretTopicAsStateUpdate(topic, payload);
        }
    }

    private void TryInterpretTopicAsStateUpdate(string deviceName, string payload)
    {
        if (friendlyNameToIdMapping.TryGetValue(deviceName, out var id))
        {
            InstanceContainer
                .Instance
                .DeviceStateManager
                .SetMultipleStates(id, ReplaceCustomStates(id, JsonConvert.DeserializeObject<Dictionary<string, JToken>>(payload)!));

            InstanceContainer
                .Instance
                .DeviceStateManager
                .SetSingleState(id, "lastReceived", DateTime.UtcNow);
        }
    }

    private Dictionary<string, JToken> ReplaceCustomStates(long id, Dictionary<string, JToken> customStates)
    {
        if (!IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var dev) || dev is not Zigbee2MqttDevice zdev)
            return customStates;

        foreach (var item in GetFeatures<Zigbee2MqttBinaryFeature>(zdev.device.Definition.Exposes))
        {
            if (customStates.TryGetValue(item.Property, out var token))
            {
                try
                {
                    customStates[item.Name] = item.ConvertToBool(token.ToOObject());
                }
                catch (Exception e)
                {
                    logger.Warn("Could not convert {0}. \r\n Reson: {1}", item.Name, e.ToString());
                }
            }
        }

        customStates = zdev.ConvertStates(customStates);

        return customStates;
    }

    private IEnumerable<T> GetFeatures<T>(Zigbee2MqttGenericExposedFeature[] feature) where T : Zigbee2MqttGenericExposedFeature
    {
        foreach (var item in feature)
        {
            if (item is T t)
            {
                yield return t;
                continue;
            }

            if (item.Features is null)
                continue;

            foreach (var item2 in GetFeatures<T>(item.Features))
            {
                yield return item2;
            }
        }
    }


    public async ValueTask DisposeAsync()
    {
        var local = MQTTClient;
        if (local is not null)
        {
            await local.StopAsync();
            local.Dispose();
        }
    }
}
