﻿using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("Plug 01", "AB3257001NJ")]
public class OsramPlug : ZigbeeSwitch
{
    public OsramPlug(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(OsramPlug))
    {
        ShowInApp = true;
    }
}
