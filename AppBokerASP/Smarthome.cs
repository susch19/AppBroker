﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AppBokerASP.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace AppBokerASP
{
    public class SmartHome : Hub
    {

        public static List<IClientProxy> ConnectedClients { get; internal set; } = new List<IClientProxy>();

        public SmartHome()
        {
        }

        public override Task OnConnectedAsync()
        {
            ConnectedClients.Add(Clients.Caller);
            foreach (var item in Program.DeviceManager.Devices.Values)
                item.SendLastData(Clients.Caller);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedClients.Remove(Clients.Caller);
            return base.OnDisconnectedAsync(exception);
        }

        public void Update(Rootobject message)
        {
            if (Program.DeviceManager.Devices.TryGetValue(message.id, out var device))
            {
                Console.WriteLine($"User send command {message.Command} to {device} with {message.Parameters}");
                device.UpdateFromApp(message.Command, message.Parameters);
            }
        }

        public async void SendUpdate(Device device)
        {
        }

        public List<Device> GetAllDevices() => Program.DeviceManager.Devices.Select(x => x.Value).ToList();

        public void Subscribe(uint DeviceId)
        {
            Console.WriteLine("User subscribed to " + DeviceId);
            var highlightedItemProperty = Clients.Caller.GetType().GetRuntimeFields().FirstOrDefault(pi => pi.Name == "_connectionId");
            string connectionId = (string)highlightedItemProperty.GetValue(Clients.Caller);

            if (Program.DeviceManager.Devices.TryGetValue(DeviceId, out var device))
                if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                    device.Subscribers.Add(new Subscriber { ConnectionId = connectionId, ClientProxy = Clients.Caller });
        }
    }
}