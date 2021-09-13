using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee
{
    public class OsramB40RW : ZigbeeLamp
    {
        public OsramB40RW(long nodeId, SocketIO socket) : base(nodeId, typeof(OsramB40RW), socket)
        {
        }
    }
}
