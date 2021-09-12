using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class FloaltPanel : ZigbeeLamp
    {
        public FloaltPanel(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
        }
    }
}
