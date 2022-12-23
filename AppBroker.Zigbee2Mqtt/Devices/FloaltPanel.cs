using AppBroker.Core.Devices;


namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("L1529")]
public class FloaltPanel : ZigbeeLamp
{
    public FloaltPanel(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(FloaltPanel))
    {
    }

}
