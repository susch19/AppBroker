using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.Core;
[JsonConverter(typeof(StringEnumConverter))]
public enum Command
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
    OtaPart

};
