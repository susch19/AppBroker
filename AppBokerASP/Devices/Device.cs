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
        public virtual async void SendLastData(IClientProxy client)
        {
            await client.SendAsync("Update", this);
        }
        public virtual void SendLastData(List<IClientProxy> clients)
        {
            clients.ForEach(async x => await x.SendAsync("Update", this));
        }

    }
}