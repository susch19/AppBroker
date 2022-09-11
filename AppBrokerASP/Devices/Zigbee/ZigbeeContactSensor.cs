using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("MCCGQ11LM")]

public partial class ZigbeeContactSensor : ZigbeeDevice
{
    public ZigbeeContactSensor(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(ZigbeeContactSensor))
    {
        ShowInApp = true;
    }
}
