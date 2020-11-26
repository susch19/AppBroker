using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AppBokerASP.Database;

using AppBroker.Core.Devices;
using AppBroker.IOBroker.Data;
using AppBroker.PainlessMesh.Data;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP
{

    public class SmartHome : Hub
    {
        public static List<IClientProxy> ConnectedClients { get; internal set; } = new List<IClientProxy>();

        private readonly SignalRMethods signalRMethods;

        public SmartHome()
        {
            signalRMethods = new SignalRMethods();
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

        public void Update(GeneralSmarthomeMessage message) => signalRMethods.Update(message);

        public void UpdateDevice(long id, string newName) => signalRMethods.UpdateDevice(id, newName);

        public dynamic GetConfig(uint deviceId) => signalRMethods.GetConfig(deviceId);

        public async void SendUpdate(Device device) => signalRMethods.SendUpdate(device);


        public List<Device> GetAllDevices() => signalRMethods.GetAllDevices();

        public List<IoBrokerHistory> GetIoBrokerHistories(long id, string dt)
            => signalRMethods.GetIoBrokerHistories(id, dt);

        public IoBrokerHistory GetIoBrokerHistory(long id, string dt, string propertyName)
            => signalRMethods.GetIoBrokerHistory(id, dt, propertyName);

        public List<IoBrokerHistory> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
            => signalRMethods.GetIoBrokerHistoriesRange(id, dt, dt2);
        public List<IoBrokerHistory> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
            => signalRMethods.GetIoBrokerHistoryRange(id, dt, dt2, propertyName);

        public List<Device> Subscribe(IEnumerable<long> deviceIds)
            => signalRMethods.Subscribe(deviceIds, Context.ConnectionId, Clients.Caller);

        public void UpdateTime() => signalRMethods.UpdateTime();
    }
}