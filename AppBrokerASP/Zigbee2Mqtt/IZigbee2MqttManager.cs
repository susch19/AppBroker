using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json.Linq;

namespace AppBrokerASP.Zigbee2Mqtt;
public interface IZigbee2MqttManager
{
    IManagedMqttClient? MQTTClient { get; set; }

    Task<IManagedMqttClient> Connect();
    Task<bool> SetStateOnDevice(string deviceName, string propertyName, JToken newValue);
    Task Subscribe();
}