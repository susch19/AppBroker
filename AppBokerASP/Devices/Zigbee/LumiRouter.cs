using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class LumiRouter : ZigbeeDevice
    {
        public LumiRouter(long nodeId) : base(nodeId, typeof(LumiRouter))
        {
            ShowInApp = false;
        }
    }
}
