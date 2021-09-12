using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class OsramPlug : ZigbeeSwitch
    {
        public OsramPlug(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = true;
        }
    }
}
