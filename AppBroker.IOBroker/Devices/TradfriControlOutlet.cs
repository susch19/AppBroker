using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("E1603/E1702")]
public class TradfriControlOutlet : ZigbeeSwitch
{
    public TradfriControlOutlet(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(TradfriControlOutlet))
    {
    }
}
