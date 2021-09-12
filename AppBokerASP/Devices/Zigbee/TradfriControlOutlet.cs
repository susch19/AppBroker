using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class TradfriControlOutlet : ZigbeeSwitch
    {
        public TradfriControlOutlet(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
        }
    }
}
