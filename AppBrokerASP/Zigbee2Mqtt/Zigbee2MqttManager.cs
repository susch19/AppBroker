using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Newtonsoft.Json;
using NLog;
using AppBrokerASP.Configuration;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Server.IISIntegration;
using System.Globalization;

namespace AppBrokerASP.Zigbee2Mqtt;

public class Zigbee2MqttManager : IAsyncDisposable
{
    private IManagedMqttClient? mqtt;
    Device[]? devices = null;
    private readonly Zigbee2MqttConfig config;
    private readonly Logger logger;
    private readonly Dictionary<string, long> friendlyNameToIdMapping = new();
    private readonly Stack<(string, string)> cachedBeforeConnect = new();

    public Zigbee2MqttManager(Zigbee2MqttConfig zigbee2MqttConfig)
    {
        config = zigbee2MqttConfig;
        logger = LogManager.GetCurrentClassLogger();
    }

    public async Task Subscribe()
    {
        if (mqtt is null)
            return;

        try
        {
            await mqtt.SubscribeAsync("zigbee2mqtt/#");

        }
        catch (Exception ex)
        {
            ;
        }
    }

    public async Task<IManagedMqttClient> Connect()
    {
        if (mqtt is not null)
            return mqtt;

        var mqttFactory = new MqttFactory();
        var managedMqttClient = mqttFactory.CreateManagedMqttClient();
        var mqttClientOptions = new MqttClientOptionsBuilder()
                        .WithTcpServer(config.Address, config.Port)
                        .Build();

        var managedMqttClientOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(mqttClientOptions)
            .Build();

        await managedMqttClient.StartAsync(managedMqttClientOptions);
        mqtt = managedMqttClient;

        mqtt.ApplicationMessageReceivedAsync += Mqtt_ApplicationMessageReceivedAsync;

        return mqtt;
    }

    private Task Mqtt_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic = e.ApplicationMessage.Topic;
        var payload = e.ApplicationMessage.ConvertPayloadToString();

        switch (topic)
        {
            case "zigbee2mqtt/bridge/devices":
                devices = JsonConvert.DeserializeObject<Device[]>(payload);
                foreach (var item in devices!)
                {
                    var id = long.Parse(item.IEEEAddress[2..], NumberStyles.HexNumber);
                    if (item.Type == DeviceType.Coordinator || IInstanceContainer.Instance.DeviceManager.Devices.ContainsKey(id))
                        continue;
                    var dev = new Zigbee2MqttDevice(item, id, mqtt!);
                    InstanceContainer.Instance.DeviceManager.AddNewDevice(dev);
                    friendlyNameToIdMapping[item.FriendlyName] = dev.Id;
                }
                while (cachedBeforeConnect.TryPop(out var item))
                    TryInterpretTopicAsStateUpdate(item.Item1, item.Item2);

                break;

            case "zigbee2mqtt/bridge/config":
                var b = JsonConvert.DeserializeObject<BridgeConfig>(payload);
                ;
                break;

            case "zigbee2mqtt/bridge/info":
                var c = JsonConvert.DeserializeObject<BridgeInfo>(payload);
                ;
                break;

            case "zigbee2mqtt/bridge/groups":
                var d = JsonConvert.DeserializeObject<Group[]>(payload);
                ;
                break;

            case "zigbee2mqtt/bridge/state":
                _ = Enum.TryParse<BridgeState>(payload, out var f);
                ;
                break;

            case "zigbee2mqtt/bridge/logging":
                var g = JsonConvert.DeserializeObject<LogMessage>(payload);
                var lvl = g!.Level switch
                {
                    LogLevel.Error => NLog.LogLevel.Error,
                    LogLevel.Warning => NLog.LogLevel.Warn,
                    LogLevel.Info => NLog.LogLevel.Info,
                    _ => NLog.LogLevel.Info,
                };
                _ = logger.ForLogEvent(lvl).Message(g.Message).Callsite();

                break;

            case "zigbee2mqtt/bridge/extensions":
            default:
                Console.WriteLine($"[{topic}] {payload}");
                if (devices is null)
                {
                    cachedBeforeConnect.Push((topic, payload));
                    break;
                }
                TryInterpretTopicAsStateUpdate(topic, payload);
                break;
        }

        return Task.CompletedTask;
    }

    private void TryInterpretTopicAsStateUpdate(string topic, string payload)
    {
        var zigbeeLength = "zigbee2mqtt/".Length;
        if (topic.Length > zigbeeLength)
        {
            var maybeFriendlyName = topic["zigbee2mqtt/".Length..];
            if (friendlyNameToIdMapping.TryGetValue(maybeFriendlyName, out var id))
            {
                InstanceContainer
                    .Instance
                    .DeviceStateManager
                    .PushNewState(id, JsonConvert.DeserializeObject<Dictionary<string, JToken>>(payload)!);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        var local = mqtt;
        if (local is not null)
        {
            await local.StopAsync();
            local.Dispose();
        }
    }
}
