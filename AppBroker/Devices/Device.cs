using System;
using System.Collections.Generic;

namespace AppBroker.Devices
{
    public abstract class Device
    {
        public int Id;
        public string Url;
        public List<string> PrintableInformation = new List<string>();
        public List<Subscriber> Subscribers = new List<Subscriber>();

        public abstract void Update(string command, string[] parameter);
    }
}