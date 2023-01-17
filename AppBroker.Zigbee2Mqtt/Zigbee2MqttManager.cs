﻿using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Newtonsoft.Json;
using NLog;

using Newtonsoft.Json.Linq;
using System.Globalization;
using AppBroker.Core;
using AppBroker.Core.Database;
using AppBrokerASP;

using ZigbeeConfig = AppBroker.Zigbee2Mqtt.Zigbee2MqttConfig;
using AppBroker.Zigbee2Mqtt.Devices;
using System.Diagnostics.CodeAnalysis;

namespace AppBroker.Zigbee2Mqtt;

public class Zigbee2MqttManager : IAsyncDisposable, IZigbee2MqttManager
{
    public event EventHandler<MqttClientConnectResult> Connected;
    public event EventHandler<MqttClientConnectResult> Disconnected;

    public event EventHandler DevicesReceived;

    public IManagedMqttClient? MQTTClient { get; set; }
    Zigbee2MqttDeviceJson[]? devices;
    private readonly ZigbeeConfig config;
    private readonly Logger logger;
    private readonly Dictionary<string, long> friendlyNameToIdMapping = new();
    private readonly Stack<(string, string, string)> cachedBeforeConnect = new();

    public Zigbee2MqttManager(ZigbeeConfig zigbee2MqttConfig)
    {
        config = zigbee2MqttConfig;
        logger = LogManager.GetCurrentClassLogger();

        IInstanceContainer.Instance.JavaScriptEngineManager.ExtendedFunctions["setValueZigbee"] = SetValue;
        IInstanceContainer.Instance.JavaScriptEngineManager.ExtendedFunctions["sendToZigbee"] = EnqueueToZigbee;
    }

    public async Task Subscribe()
    {
        if (MQTTClient is null)
            return;

        try
        {
            logger.Debug("Subscribing to zigbee2mqtt topic");
            await MQTTClient.SubscribeAsync("zigbee2mqtt/#");

        }
        catch (Exception ex)
        {
            logger.Error("Erorr during subscribing to zigbee2mqtt topic", ex);
        }
    }

    public Task SetOption(string name, string propName, JToken value)
    {

        return MQTTClient.EnqueueAsync("zigbee2mqtt/bridge/request/device/options", $$"""{"id":{{name}}, "options":{"{{propName}}":{{value}}} }""");
    }

    public Task SetValue(string name, string propName, JToken newValue)
    {
        //TODO: Evil hack for other system cultures
        string stringifiedValue;
        if (newValue.Type == JTokenType.Float)
        {
            stringifiedValue = newValue.ToString().Replace("\"", "").Replace(",", ".");
        }
        else
        {
            stringifiedValue = newValue.ToString();
        }
        return MQTTClient.EnqueueAsync($"zigbee2mqtt/{name}/set/{propName}", stringifiedValue);
    }
    public Task EnqueueToZigbee(string path, JToken payload)
    {
        return MQTTClient.EnqueueAsync($"zigbee2mqtt/{path}", payload.ToString());
    }

    public async Task<IManagedMqttClient> Connect()
    {
        logger.Debug("Connecting to mqtt");
        if (MQTTClient is not null)
        {
            logger.Debug("Already connected to mqtt, returing existing instance");
            return MQTTClient;
        }

        var randomId = Guid.NewGuid().ToString("N")[..8];

        var mqttFactory = new MqttFactory();
        var managedMqttClient = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer(config.Address, config.Port)
                        .WithClientId($"appbroker_{randomId}");

        logger.Debug("Builded new mqtt tcp server options");

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();

        logger.Debug("Builded new mqtt tcp client options");

        MQTTClient = managedMqttClient;
        MQTTClient.ApplicationMessageReceivedAsync += Mqtt_ApplicationMessageReceivedAsync;

        MQTTClient.ConnectedAsync
            += (args) =>
            {
                Connected?.Invoke(this, args.ConnectResult);
                return Task.CompletedTask;
            };

        MQTTClient.DisconnectedAsync
            += (args) =>
            {

                Disconnected?.Invoke(this, args.ConnectResult);
                return Task.CompletedTask;
            };

        logger.Debug("Subscribed the incomming mqtt messages");
        await managedMqttClient.StartAsync(managedMqttClientOptions);
        logger.Debug("Started the mqtt client");

        return MQTTClient;
    }


    private async Task Mqtt_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        if (!topic.StartsWith("zigbee2mqtt/"))
        {
            return;
        }

        var payload = e.ApplicationMessage.ConvertPayloadToString();

        var splitted = topic.Split('/', 3);

        var deviceName = splitted[1];
        if (splitted.Length < 3)
        {
            Console.WriteLine($"[{topic}] {payload}");
            if (devices is null)
            {
                if (config.RestartOnMissingDevice)
                    await MQTTClient.EnqueueAsync("zigbee2mqtt/bridge/request/restart");
                else
                    await MQTTClient.SubscribeAsync("zigbee2mqtt/#");

                logger.Trace($"Got state before device {deviceName}");
                cachedBeforeConnect.Push((topic, deviceName, payload));
                return;
            }
            TryInterpretTopicAsStateUpdate(topic, deviceName, payload);
            return;

        }

        var method = splitted[2];
        switch (method)
        {
            case "devices":
                devices = JsonConvert.DeserializeObject<Zigbee2MqttDeviceJson[]>(payload);
                using (var ctx = DbProvider.BrokerDbContext)
                {

                    foreach (var item in devices!)
                    {
                        var id = long.Parse(item.IEEEAddress[2..], NumberStyles.HexNumber);
                        logger.Debug($"Trying to create new device {id}");
                        var dbDevice = ctx.Devices.FirstOrDefault(x => x.Id == id);
                        if (dbDevice is not null && !string.IsNullOrWhiteSpace(dbDevice.FriendlyName) && dbDevice.FriendlyName != item.FriendlyName)
                        {
                            logger.Info($"Friendly name of Zigbee2Mqtt Device {item.FriendlyName} does not match saved name {dbDevice.FriendlyName}, updating");
                            await MQTTClient.EnqueueAsync("zigbee2mqtt/bridge/request/device/rename", $"{{\"from\": \"{item.IEEEAddress}\", \"to\": \"{dbDevice.FriendlyName}\"}}");
                            item.FriendlyName = dbDevice.FriendlyName;
                        }

                        friendlyNameToIdMapping[item.FriendlyName] = id;
                        if (item.Type == Zigbee2MqttDeviceType.Coordinator ||
                            IInstanceContainer.Instance.DeviceManager.Devices.ContainsKey(id))
                        {
                            logger.Debug($"Already having device {id} or is coordinator");
                            continue;
                        }

                        var dev = IInstanceContainer.Instance.DeviceTypeMetaDataManager.CreateDeviceFromNameWithBaseType(Zigbee2MqttDevice.GetTypeName(item), typeof(Zigbee2MqttDevice), typeof(Zigbee2MqttDevice), item!, id);

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
                    logger.Debug($"Popped state for {item.Item2}, {cachedBeforeConnect.Count} left to go");
                    TryInterpretTopicAsStateUpdate(item.Item1, item.Item2, item.Item3);
                }

                DevicesReceived?.Invoke(this, EventArgs.Empty);
                break;

            case "config":
                //var b = JsonConvert.DeserializeObject<Zigbee2MqttBridgeConfig>(payload);
                ;
                break;

            case "info":
                //var c = JsonConvert.DeserializeObject<Zigbee2MqttBridgeInfo>(payload);
                ;
                break;

            case "groups":
                //var d = JsonConvert.DeserializeObject<Zigbee2MqttGroup[]>(payload);
                break;
            //case "state":
            //    _ = Enum.TryParse<Zigbee2MqttAvailabilityState>(payload, out var f);
            //    ;
            //    break;
            case "logging":
                var g = JsonConvert.DeserializeObject<Zigbee2MqttLogMessage>(payload);
                var lvl = g!.Level switch
                {
                    Zigbee2MqttLogLevel.Error => LogLevel.Error,
                    Zigbee2MqttLogLevel.Warning => LogLevel.Warn,
                    Zigbee2MqttLogLevel.Info => LogLevel.Info,
                    _ => LogLevel.Info,
                };
                _ = logger.ForLogEvent(lvl).Message(g.Message).Callsite();

                break;
            //"zigbee2mqtt/MY_DEVICE/availability"
            case "availability":
                if (friendlyNameToIdMapping.TryGetValue(deviceName, out var deviceId))
                {
                    InstanceContainer
                        .Instance
                        .DeviceStateManager
                        .PushNewState(deviceId, "available", payload == "online");
                }
                else
                {
                    logger.Warn($"Couldn't set availability ({payload}) on {deviceName}");
                    /*
                     *
2022-12-20 20:29:35.9043|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Katzenfütterstelle 💡
2022-12-20 20:29:35.9058|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Wohnzimmer FLOALT panel 💡
2022-12-20 20:29:35.9058|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Büro FLOALT panel  💡
2022-12-20 20:29:35.9058|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Esszimmer Switch
2022-12-20 20:29:35.9091|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Wohnzimmer Fernbedienung
2022-12-20 20:29:35.9091|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Esszimmer Fensterbank 💡
2022-12-20 20:29:35.9091|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Büro Fernbedienung
2022-12-20 20:29:35.9091|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Badezimmer 🛀
2022-12-20 20:29:35.9134|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Wohnzimmer 🛋️
2022-12-20 20:29:35.9134|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Sascha Schlafzimmer 🌡
2022-12-20 20:29:35.9179|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Sascha Zimmer
2022-12-20 20:29:35.9179|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Küche | Esszimmer 🍽️
2022-12-20 20:29:35.9179|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Büro ☎️
2022-12-20 20:29:35.9212|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Papa Schlafzimmer 🛌
2022-12-20 20:29:35.9212|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Eingangsbereich 🥾
2022-12-20 20:29:35.9212|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Papa Badezimmer 🚿
2022-12-20 20:29:35.9212|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Patrick ♒ Zimmer
2022-12-20 20:29:35.9257|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (offline) on Papa Schlafzimmer
2022-12-20 20:29:35.9257|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (offline) on Esszimmer Deckenleuchte
2022-12-20 20:29:35.9257|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Sascha Schlafzimmer 💡
2022-12-20 20:29:35.9292|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Sascha Zimmer Strom
2022-12-20 20:29:35.9292|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Kühlschrank
2022-12-20 20:29:35.9292|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Waschraum Trockner
2022-12-20 20:29:35.9292|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on Waschraum Waschmaschine
2022-12-20 20:29:35.9335|WARN|AppBroker.Zigbee2Mqtt.Zigbee2MqttManager|Couldn't set availability (online) on 901
                     *
                     */
                }
                break;
            case "extensions":
            default:
                break;
        }

        return;
    }

    private void TryInterpretTopicAsStateUpdate(string topic, string deviceName, string payload)
    {
        var zigbeeLength = "zigbee2mqtt/".Length;
        if (topic.Length > zigbeeLength)
        {
            if (friendlyNameToIdMapping.TryGetValue(deviceName, out var id))
            {
                InstanceContainer
                    .Instance
                    .DeviceStateManager
                    .PushNewState(id, ReplaceCustomStates(id, JsonConvert.DeserializeObject<Dictionary<string, JToken>>(payload)!));

                InstanceContainer
                    .Instance
                    .DeviceStateManager
                    .PushNewState(id, "lastReceived", DateTime.Now);
            }
        }
    }

    private Dictionary<string, JToken> ReplaceCustomStates(long id, Dictionary<string, JToken> customStates)
    {
        if (!IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var dev) || dev is not Zigbee2MqttDevice zdev)
            return customStates;

        foreach (var item in GetFeatures<Zigbee2MqttBinaryFeature>(zdev.device.Definition.Exposes))
        {
            if (customStates.TryGetValue(item.Property, out var token) && token.Type != JTokenType.Boolean)
            {
                customStates[item.Name] = item.ConvertToBool(token.ToOObject());
            }
        }
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

    public async Task<bool> SetStateOnDevice(string deviceName, string propertyName, JToken newValue)
    {
        if (MQTTClient is null)
            return false;

        logger.Info($"Updating device {deviceName} state {propertyName} with new value {newValue}");
        await MQTTClient.EnqueueAsync($"zigbee2mqtt/{deviceName}/set/{propertyName}", newValue.ToString());
        return true;
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
