namespace PainlessMesh
{
    public enum PackageType
    {
        DROP = 3,
        SYNC_PACKAGE = 4,
        TIME_SYNC = 4,
        NODE_SYNC_REQUEST = 5,
        NODE_SYNC_REPLY = 6,
        BROADCAST = 8,  //application data for everyone
        SINGLE = 9,   //application data for a single node
        BRIDGE = 255, 
    };

    public enum MessageType
    {
        Get, 
        Update,
        Options,
        Relay
    };

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
