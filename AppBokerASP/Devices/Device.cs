using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;

namespace AppBokerASP.Devices
{
    public abstract class Device
    {

        public uint Id { get; set; }
        public List<string> PrintableInformation { get; set; } = new List<string>();
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
        public string TypeName { get; set; }

        public Device(uint nodeId)
        {
            Id = nodeId;
        }

        public virtual void UpdateFromApp(Command command, List<JsonElement> parameter) { }
        public virtual void OptionsFromApp(Command command, List<JsonElement> parameter) { }
        public virtual void StopDevice() { }
        public virtual dynamic GetConfig() { return null; }
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