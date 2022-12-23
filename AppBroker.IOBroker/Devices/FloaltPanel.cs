using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("FLOALT panel WS 60x60", "L1529", "T2011")]
public class FloaltPanel : ZigbeeLamp
{
    public FloaltPanel(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(FloaltPanel))
    {
    }

}
