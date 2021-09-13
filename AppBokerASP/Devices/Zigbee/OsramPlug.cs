using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    [DeviceName("Plug 01", "AB3257001NJ")]
    public class OsramPlug : ZigbeeSwitch
    {
        public OsramPlug(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = true;
        }
    }
}
