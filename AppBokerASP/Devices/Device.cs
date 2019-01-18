using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP.Devices
{
    public abstract class Device
    {
        public uint Id;
        public List<string> PrintableInformation = new List<string>();
        public List<Subscriber> Subscribers = new List<Subscriber>();
        public string TypeName;

        public abstract void UpdateFromApp(string command, List<string> parameter);
    }
}