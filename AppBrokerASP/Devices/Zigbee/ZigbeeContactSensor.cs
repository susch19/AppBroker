﻿using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("MCCGQ11LM")]

[AppBroker.ClassPropertyChangedAppbroker]
public partial class ZigbeeContactSensor : ZigbeeDevice
{

    private double temperature;
    private bool state;
    public ZigbeeContactSensor(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }
}