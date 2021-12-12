﻿using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("T2011")]
public class OsvallaPanel : ZigbeeLamp
{
    public OsvallaPanel(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
    }
}
