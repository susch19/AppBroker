﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.Core;
public enum Command : int
{
    None,
    Off,
    On,
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
    RGB,
    Strobo,
    RGBCycle,
    LightWander,
    RGBWander,
    Reverse,
    SingleColor,
    DeviceMapping,
    Calibration,
    Ota,
    OtaPart,
    Log,
    Zigbee = 100

};
