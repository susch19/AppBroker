using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    public class OsramB40RW : ZigbeeLamp
    {
        public OsramB40RW(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
        }
    }
}
