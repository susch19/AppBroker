﻿using Newtonsoft.Json;
using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("E1525/E1745")]
public partial class TradfriMotionSensor : ZigbeeDevice
{
    private byte battery;
    private long noMotion;
    private bool occupancy;

    public TradfriMotionSensor(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(TradfriMotionSensor))
    {
        ShowInApp = true;
    }
}
