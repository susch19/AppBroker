using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("MCCGQ11LM")]

public partial class ZigbeeContactSensor : Zigbee2MqttDevice
{
    public ZigbeeContactSensor(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(ZigbeeContactSensor))
    {
        ShowInApp = true;
    }
}
