using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("Plug 01", "AB3257001NJ")]
public class OsramPlug : ZigbeeSwitch
{
    public OsramPlug(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(OsramPlug))
    {
        ShowInApp = true;
    }
}
