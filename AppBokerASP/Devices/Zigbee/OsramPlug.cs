using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class OsramPlug : UpdateableZigbeeDevice
    {
        public bool State { get; set; }

        public OsramPlug(long nodeId, string baseUpdateUrl, SocketIO socket) : base(nodeId, baseUpdateUrl, typeof(OsramPlug), socket)
        {
            ShowInApp = true;
        }
    }
}
