using AppBroker.Core.Devices;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("E1603/E1702")]
public class TradfriControlOutlet : ZigbeeSwitch
{
    public TradfriControlOutlet(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(TradfriControlOutlet))
    {
    }
}
