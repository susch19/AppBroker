using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("E1603/E1702", "E1603/E1702/E1708", "E1603", "E1702", "E1708")]
public class TradfriControlOutlet : ZigbeeSwitch
{
    public TradfriControlOutlet(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(TradfriControlOutlet))
    {
    }
}
