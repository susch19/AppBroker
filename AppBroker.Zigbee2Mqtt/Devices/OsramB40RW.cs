using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("Classic B40 TW - LIGHTIFY", "AB32840")]
public class OsramB40RW : ZigbeeLamp
{
    public OsramB40RW(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId, nameof(OsramB40RW))
    {
    }
}
