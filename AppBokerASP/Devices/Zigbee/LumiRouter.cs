using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    [DeviceName("lumi.router")]
    public class LumiRouter : ZigbeeDevice
    {
        public LumiRouter(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = false;
        }
    }
}
