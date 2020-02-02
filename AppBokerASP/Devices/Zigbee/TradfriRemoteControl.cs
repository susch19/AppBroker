using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class TradfriRemoteControl : ZigbeeDevice
    {
        public TradfriRemoteControl(long nodeId) : base(nodeId, typeof(TradfriRemoteControl))
        {
            ShowInApp = false;

        }
    }
}
