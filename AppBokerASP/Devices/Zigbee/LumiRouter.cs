using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class LumiRouter : ZigbeeDevice
    {
        public LumiRouter(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = false;
        }
    }
}
