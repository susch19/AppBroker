using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class TradfriRemoteControl : ZigbeeDevice
    {
        public TradfriRemoteControl(long nodeId, SocketIO socket) : base(nodeId, typeof(TradfriRemoteControl), socket)
        {
            ShowInApp = false;

        }
    }
}
