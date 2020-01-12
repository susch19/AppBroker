using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Devices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;

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

        public void Update(GeneralSmarthomeMessage message)
        {
            if (Program.DeviceManager.Devices.TryGetValue(message.NodeId, out var device))
            {
                switch (message.MessageType)
                {
                    case MessageType.Get:
                        break;
                    case MessageType.Update:
                        device.UpdateFromApp(message.Command, message.Parameters);
                        break;
                    case MessageType.Options:
                        device.OptionsFromApp(message.Command, message.Parameters);
                        break;
                    default:
                        break;
                }
                //Console.WriteLine($"User send command {message.Command} to {device} with {message.Parameters}");
            }
        }

        public void UpdateDevice(ulong id, string newName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var stored))
            {
                stored.FriendlyName = newName;
                DbProvider.UpdateDeviceInDb(stored);
                stored.SendDataToAllSubscribers();
            }
        }

        public dynamic GetConfig(uint deviceId)
        {
            if (Program.DeviceManager.Devices.TryGetValue(deviceId, out var device))
            {
                return device.GetConfig();
                //Console.WriteLine($"User send command {message.Command} to {device} with {message.Parameters}");
            }
            return null;
        }

        public async void SendUpdate(Device device)
        {
            foreach (var client in ConnectedClients)
            {
                await client.SendAsync("Update", device);
            }
        }

        public List<Device> GetAllDevices() => Program.DeviceManager.Devices.Select(x => x.Value).Where(x=>x.ShowInApp).ToList();


        public Device Subscribe(ulong DeviceId)
        {
            Console.WriteLine("User subscribed to " + DeviceId);
            var highlightedItemProperty = Clients.Caller.GetType().GetRuntimeFields().FirstOrDefault(pi => pi.Name == "_connectionId");
            string connectionId = (string)highlightedItemProperty.GetValue(Clients.Caller);

            if (Program.DeviceManager.Devices.TryGetValue(DeviceId, out var device))
            {
                if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                    device.Subscribers.Add(new Subscriber { ConnectionId = connectionId, ClientProxy = Clients.Caller });
                return device;
            }
            return null;
        }

        public void UpdateTime()
        {
            Program.MeshManager.UpdateTime();
        }
    }
}