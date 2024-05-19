using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("SPZB0001")]
public class Dummy : UpdateableZigbeeDevice
{
    public Dummy(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(Dummy))
    {
    }
}
