using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Newtonsoft.Json;
using NLog;
using AppBrokerASP.Configuration;

namespace AppBrokerASP.Zigbee2Mqtt;

public class Zigbee2MqttManager : IAsyncDisposable
{
    private IManagedMqttClient? mqtt;
    Device[]? devices = null;
    private readonly Zigbee2MqttConfig config;
    private readonly Logger logger;

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
                ;
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
                break;
        }

        return Task.CompletedTask;
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
