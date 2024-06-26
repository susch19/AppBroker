﻿using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("TRADFRI bulb E27 CWS opal 600lm", "TRADFRI bulb E14 CWS opal 600lm", "LED1624G9")]
public partial class TradfriLedBulb : ZigbeeLamp
{
    public TradfriLedBulb(long nodeId, SocketIOClient.SocketIO socket) :
        base(nodeId, socket, nameof(TradfriLedBulb))
    {
        ShowInApp = true;
    }
}
