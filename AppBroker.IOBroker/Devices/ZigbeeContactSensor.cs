using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("MCCGQ11LM")]

public partial class ZigbeeContactSensor : ZigbeeDevice
{
    public ZigbeeContactSensor(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(ZigbeeContactSensor))
    {
        ShowInApp = true;
    }
}
