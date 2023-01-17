using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json.Linq;

namespace AppBroker.Zigbee2Mqtt;
public interface IZigbee2MqttManager
{
    IManagedMqttClient? MQTTClient { get; set; }

    event EventHandler<MqttClientConnectResult> Connected;
    event EventHandler DevicesReceived;
    event EventHandler<MqttClientConnectResult> Disconnected;

    Task<IManagedMqttClient> Connect();
    Task EnqueueToZigbee(string path, JToken payload);
    Task SetOption(string name, string propName, JToken value);
    Task<bool> SetStateOnDevice(string deviceName, string propertyName, JToken newValue);
    Task SetValue(string name, string propName, JToken newValue);
    Task Subscribe();
}