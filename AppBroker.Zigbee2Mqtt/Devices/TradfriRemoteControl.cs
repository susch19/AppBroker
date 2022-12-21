using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("TRADFRI remote control", "E1524/E1810")]
public class TradfriRemoteControl : Zigbee2MqttDevice
{
    public TradfriRemoteControl(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = false;
    }
}
