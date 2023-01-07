using AppBroker.Core.Devices;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.maxi.Devices;

internal class RoomDevice : Device
{
    public RoomDevice(long nodeId) : base(nodeId, "room_main_device")
    {
        ShowInApp = true;
    }
}
