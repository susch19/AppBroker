using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    [DeviceName("TRADFRI remote control", "E1524/E1810")]
    public class TradfriRemoteControl : ZigbeeDevice
    {
        public TradfriRemoteControl(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = false;
        }
    }
}
