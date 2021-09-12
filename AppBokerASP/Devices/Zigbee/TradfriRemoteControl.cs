using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class TradfriRemoteControl : ZigbeeDevice
    {
        public TradfriRemoteControl(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = false;
        }
    }
}
