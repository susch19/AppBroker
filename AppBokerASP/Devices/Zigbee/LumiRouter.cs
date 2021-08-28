using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class LumiRouter : ZigbeeDevice
    {
        public LumiRouter(long nodeId, SocketIO socket) : base(nodeId, typeof(LumiRouter), socket)
        {
            ShowInApp = false;
        }
    }
}
