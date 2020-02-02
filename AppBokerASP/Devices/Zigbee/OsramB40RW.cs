using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class OsramB40RW : ZigbeeLamp
    {
        public OsramB40RW(long nodeId, string baseUpdateUrl) : base(nodeId, baseUpdateUrl, typeof(OsramB40RW))
        {
        }
    }
}
