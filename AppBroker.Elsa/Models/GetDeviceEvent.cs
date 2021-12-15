using AppBroker.Core.Devices;

using System;
using System.Collections.Generic;
using System.Text;

namespace AppBroker.Elsa.Models;

public class GetDeviceEvent
{
    public string DeviceName { get; set; }
    public string TypeName { get; set; }
    public long DeviceId { get; set; }
    public Device Device { get; set; }

    public GetDeviceEvent()
    {

    }

    public GetDeviceEvent(Device device)
    {
        Device = device;
    }
}
