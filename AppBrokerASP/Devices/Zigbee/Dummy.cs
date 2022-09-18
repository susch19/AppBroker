using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("SPZB0001")]
public class Dummy : UpdateableZigbeeDevice
{
    public Dummy(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(Dummy))
    {
    }
}
