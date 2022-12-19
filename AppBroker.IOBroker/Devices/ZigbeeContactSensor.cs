using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("MCCGQ11LM")]

public partial class ZigbeeContactSensor : ZigbeeDevice
{
    public ZigbeeContactSensor(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(ZigbeeContactSensor))
    {
        ShowInApp = true;
    }
}
