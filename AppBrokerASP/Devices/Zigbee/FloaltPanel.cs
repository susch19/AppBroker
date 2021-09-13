using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee
{
    public class FloaltPanel : ZigbeeLamp
    {
        public FloaltPanel(long nodeId, SocketIO socket) : base(nodeId, typeof(FloaltPanel), socket)
        {
        }
    }
}
