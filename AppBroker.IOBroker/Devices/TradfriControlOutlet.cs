using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("E1603/E1702")]
public class TradfriControlOutlet : ZigbeeSwitch
{
    public TradfriControlOutlet(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(TradfriControlOutlet))
    {
    }
}
