using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("FLOALT panel WS 60x60", "L1529", "T2011")]
public class FloaltPanel : ZigbeeLamp
{
    public FloaltPanel(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(FloaltPanel))
    {
    }

}
