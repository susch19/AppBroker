using System;
using System.Collections.Generic;
using System.Linq;
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
            if (Program.DeviceManager.Devices.Count < 2)
            {
                //Program.DeviceManager.Devices.Add(1, new LedStrip("http://192.168.49.57") { Id = 1 });
            }
        }

        public override Task OnConnectedAsync()
        {
            ConnectedClients.Add(Clients.Caller);
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
                device.UpdateFromApp(message.Command, message.Parameters);
            }
        }

        public async void SendUpdate(Device device)
        {
        }

        public List<Device> GetAllDevices() => Program.DeviceManager.Devices.Select(x => x.Value).ToList();
    }
}