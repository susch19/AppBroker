using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class OsramPlug : UpdateableZigbeeDevice
    {

        public OsramPlug(long nodeId, string baseUpdateUrl) : base(nodeId, baseUpdateUrl, typeof(OsramPlug))
        {
            ShowInApp = false;
        }
    }
}
