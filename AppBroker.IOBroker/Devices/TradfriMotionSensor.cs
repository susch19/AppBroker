using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("E1525/E1745")]
public partial class TradfriMotionSensor : ZigbeeDevice
{

    public TradfriMotionSensor(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(TradfriMotionSensor))
    {
        ShowInApp = true;
    }
}
