using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using AppBokerASP.Database;
using AppBokerASP.Devices;
using AppBokerASP.Devices.Zigbee;
using AppBokerASP.IOBroker;

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

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _ = ConnectedClients.Remove(Clients.Caller);
            return base.OnDisconnectedAsync(exception);
        }

        public void Update(JsonSmarthomeMessage message)
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

        public void UpdateDevice(long id, string newName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var stored))
            {
                stored.FriendlyName = newName;
                _ = DbProvider.UpdateDeviceInDb(stored);
                stored.SendDataToAllSubscribers();
            }
        }

        public dynamic? GetConfig(uint deviceId)
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

        public List<Device> GetAllDevices() => Program.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();

        public List<IoBrokerHistory> GetIoBrokerHistories(long id, string dt)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device))
                if (device is ZigbeeDevice d)
                    return d.ReadHistoryJSON(DateTime.Parse(dt));
            return new List<IoBrokerHistory>();
        }

        public IoBrokerHistory GetIoBrokerHistory(long id, string dt, string propertyName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device))
                if (device is ZigbeeDevice d)
                    return d.ReadHistoryJSON(DateTime.Parse(dt), propertyName);
            return new IoBrokerHistory();
        }

        public List<IoBrokerHistory> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device))
                if (device is ZigbeeDevice d)
                {
                    var from = DateTime.Parse(dt);
                    var to = DateTime.Parse(dt2);
                    var histories = new List<IoBrokerHistory>();
                    while (from < to)
                    {
                        histories.AddRange(d.ReadHistoryJSON(from));
                        from = from.AddDays(1);
                    }
                    return histories;
                }
            return new List<IoBrokerHistory>();
        }

        public List<IoBrokerHistory> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device))
                if (device is ZigbeeDevice d)
                {
                    var from = DateTime.Parse(dt);
                    var to = DateTime.Parse(dt2);
                    var histories = new List<IoBrokerHistory>();
                    while (from < to)
                    {
                        histories.Add(d.ReadHistoryJSON(from, propertyName));
                        from = from.AddDays(1);
                    }
                    return histories;
                }
            return new List<IoBrokerHistory>();
        }

        public List<Device> Subscribe(IEnumerable<long> DeviceIds)
        {
            var highlightedItemProperty = Clients.Caller.GetType().GetRuntimeFields().FirstOrDefault(pi => pi.Name == "_connectionId");
            string connectionId = (string)highlightedItemProperty!.GetValue(Clients.Caller)!;
            var devices = new List<Device>();
            var subMessage = "User subscribed to ";
            foreach (var deviceId in DeviceIds)
            {

                if (Program.DeviceManager.Devices.TryGetValue(deviceId, out var device))
                {
                    if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                        device.Subscribers.Add(new Subscriber(connectionId, Clients.Caller));
                    devices.Add(device);
                    subMessage += device.Id + "/" + device.FriendlyName + ", ";
                }
            }
            Console.WriteLine(subMessage);
            return devices;
        }

        public void UpdateTime()
        {
            Program.MeshManager.UpdateTime();
        }
    }
}