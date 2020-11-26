using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Data
{


    public enum Command
    {
        WhoIAm,
        IP,
        Time,
        Temp,
        Brightness,
        RelativeBrightness,
        Color,
        Mode,
        OnChangedConnections,
        OnNewConnection,
        Mesh,
        Delay,
        Off,
        RGB,
        Strobo,
        RGBCycle,
        LightWander,
        RGBWander,
        Reverse,
        SingleColor,
        DeviceMapping,
        Calibration

    };
}
