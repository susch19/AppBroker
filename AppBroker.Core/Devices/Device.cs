using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

using AppBroker.Core.Data;

using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.Devices
{
    public abstract class Device
    {
        public long Id { get; set; }
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
        public string TypeName { get; set; }
        public bool ShowInApp { get; set; }
        public string FriendlyName { get; set; }
        public bool IsConnected { get; set; }
        protected readonly NLog.Logger logger;

        public Device(long nodeId)
        {
            Id = nodeId;
            TypeName = GetType().Name;
            IsConnected = true;
            logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public virtual void UpdateFromApp(Command command, List<JToken> parameters) { }
        public virtual void OptionsFromApp(Command command, List<JToken> parameters) { }

        public virtual dynamic GetConfig() { return null; }

        public virtual async void SendLastData(IClientProxy client) => await client.SendAsync("Update", this);
        public virtual void SendLastData(List<IClientProxy> clients) => clients.ForEach(async x => await x.SendAsync("Update", this));
        public virtual void SendDataToAllSubscribers() => Subscribers.ForEach(x => SendLastData(x.ClientProxy));

        public virtual void StopDevice() => IsConnected = false;
        public virtual void Reconnect() => IsConnected = true;
    }
}