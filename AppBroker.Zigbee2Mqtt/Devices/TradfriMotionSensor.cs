using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("E1525/E1745")]
public partial class TradfriMotionSensor : Zigbee2MqttDevice
{

    public TradfriMotionSensor(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(TradfriMotionSensor))
    {
        ShowInApp = true;
    }
}
