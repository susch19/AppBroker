using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("FLOALT panel WS 60x60", "L1529")]
public class FloaltPanel : ZigbeeLamp
{
    public FloaltPanel(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
    }

}
