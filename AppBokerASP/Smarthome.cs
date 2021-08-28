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

    public interface ISmartHomeClient 
    {
        Task Update(Device device);
    }

    public class SmartHome : Hub<ISmartHomeClient>
    {


        public SmartHome()
        {
        }

        public override Task OnConnectedAsync()
        {
            foreach (var item in Program.DeviceManager.Devices.Values)
                item.SendLastData(Clients.Caller);
            return base.OnConnectedAsync();
        }

        public void Update(JsonSmarthomeMessage message)
        {
            if (Program.DeviceManager.Devices.TryGetValue(message.LongNodeId, out var device))
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

            await (Clients.All?.Update(device) ?? Task.CompletedTask);
        }

        public List<Device> GetAllDevices() => Program.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();

        public Task<List<IoBrokerHistory>> GetIoBrokerHistories(long id, string dt)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
            {
                var date = DateTime.Parse(dt).Date;
                return d.GetHistory(date, date.AddDays(1).AddSeconds(-1));
            }
            return Task.FromResult(new List<IoBrokerHistory>());
        }

        public Task<IoBrokerHistory> GetIoBrokerHistory(long id, string dt, string propertyName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
            {
                var date = DateTime.Parse(dt).Date;
                return d.GetHistory(date, date.AddDays(1).AddSeconds(-1), Enum.Parse<HistoryType>(propertyName, true));
            }
            return Task.FromResult(new IoBrokerHistory());
        }

        public Task<List<IoBrokerHistory>> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
            {
                return d.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2));
            }

            return Task.FromResult(new List<IoBrokerHistory>());
        }

        // TODO: remove list, just return one item
        public async Task<List<IoBrokerHistory>> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
        {
            if (Program.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
            {
                return new List<IoBrokerHistory>()
                {
                    await d.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2), Enum.Parse<HistoryType>(propertyName, true))
                };
            }

            return new List<IoBrokerHistory>();
        }

        public List<Device> Subscribe(IEnumerable<long> DeviceIds)
        {
            var proxyFieldInfo = Clients
                .Caller
                .GetType()
                .GetRuntimeFields()
                .First(x => x.Name == "_proxy");
            var proxy = proxyFieldInfo!.GetValue(Clients.Caller)!;
            var highlightedItemProperty =
                proxy
                .GetType()
                .GetRuntimeFields()
                .First(pi => pi.Name == "_connectionId");
            string connectionId = (string)highlightedItemProperty.GetValue(proxy)!;
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