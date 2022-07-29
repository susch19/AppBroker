﻿using SocketIOClient;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee;

public abstract class UpdateableZigbeeDevice : ZigbeeDevice
{
    public UpdateableZigbeeDevice(long nodeId, SocketIO socket) :
        base(nodeId, socket)
    {

    }

    public virtual Task SetValue(string property, object value) 
        => Socket.EmitAsync("setState", $"{AdapterWithId}.{property.ToLower()}", value);
}
