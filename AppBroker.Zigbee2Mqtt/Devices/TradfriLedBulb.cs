using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("TRADFRI bulb E27 CWS opal 600lm", "TRADFRI bulb E14 CWS opal 600lm", "LED1624G9")]
public partial class TradfriLedBulb : ZigbeeLamp
{
    public TradfriLedBulb(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = true;
    }
}
