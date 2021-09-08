﻿using SocketIOClient;
using System;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class UpdateableZigbeeDevice : ZigbeeDevice
    {
        public UpdateableZigbeeDevice(long nodeId, Type type, SocketIO socket) : 
            base(nodeId, type, socket)
        {

        }

        protected Task SetValue(string property, object value)
        {
            return Socket.EmitAsync("setState", $"{AdapterWithId}.{property.ToLower()}", value);
        }
    }
}
