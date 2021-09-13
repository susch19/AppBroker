using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public class OsramB40RW : ZigbeeLamp
    {
        public OsramB40RW(long nodeId, string baseUpdateUrl, SocketIO socket) : base(nodeId, baseUpdateUrl, typeof(OsramB40RW), socket)
        {
        }
    }
}
