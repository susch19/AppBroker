using AppBokerASP.Devices;
using Microsoft.AspNet.SignalR.Client;
using PainlessMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AppBokerASP
{
    public class CloudClient : IDisposable
    {
        public static CloudClient Client { get; private set; }
        private IHubProxy serverToServerHubProxy;
        private readonly SignalRMethods signalRMethods;
        private HubConnection hubConnection;

        public CloudClient()
        {
            Client = this;
            signalRMethods = new SignalRMethods();
        }

        public async Task CreateAndStartConnection()
        {
            hubConnection?.Dispose();

            hubConnection = new HubConnection("https://smarthome.susch.eu/");
            serverToServerHubProxy = hubConnection.CreateHubProxy("ServerToServer");
            serverToServerHubProxy.On<short, uint>("GetConfig", GetConfig);

            serverToServerHubProxy.On<short>("GetAllDevices", GetAllDevices);

            serverToServerHubProxy.On<short, long, string>("GetIoBrokerHistories", GetIoBrokerHistories);

            serverToServerHubProxy.On<short, long, string, string>("GetIoBrokerHistory", GetIoBrokerHistory);

            serverToServerHubProxy.On<short, long, string, string>("GetIoBrokerHistoriesRange", GetIoBrokerHistoriesRange);

            serverToServerHubProxy.On<short, long, string, string, string>("GetIoBrokerHistoryRange", GetIoBrokerHistoryRange);

            serverToServerHubProxy.On<GeneralSmarthomeMessage>("Update", UpdateMessage);

            serverToServerHubProxy.On<long, string>("UpdateDevice", UpdateDevice);

            serverToServerHubProxy.On<Device>("SendUpdate", SendUpdate);

            serverToServerHubProxy.On("UpdateTime", UpdateTime);

            await hubConnection.Start();
        }

        private void UpdateMessage(GeneralSmarthomeMessage obj)
        => signalRMethods.Update(obj);


        private void UpdateTime() => signalRMethods.UpdateTime();
        private void SendUpdate(Device obj) => signalRMethods.SendUpdate(obj);
        private void UpdateDevice(long arg1, string arg2) => signalRMethods.UpdateDevice(arg1, arg2);
        private void GetIoBrokerHistoryRange(short callbackId, long id, string dt, string dt2, string propertyName)
        {
            var history = signalRMethods.GetIoBrokerHistoryRange(id, dt, dt2, propertyName);
            serverToServerHubProxy.Invoke("SetIoBrokerHistoryRange", callbackId, history);
        }

        private void GetIoBrokerHistoriesRange(short callbackId, long id, string dt, string dt2)
        {
            var history = signalRMethods.GetIoBrokerHistoriesRange(id, dt, dt2);
            serverToServerHubProxy.Invoke("SetIoBrokerHistoriesRange", callbackId, history);
        }

        private void GetIoBrokerHistory(short callbackId, long id, string dt, string propertyName)
        {
            var history = signalRMethods.GetIoBrokerHistory(id, dt, propertyName);
            serverToServerHubProxy.Invoke("SetIoBrokerHistory", callbackId, history);
        }

        private void GetIoBrokerHistories(short callbackId, long id, string dt)
        {
            var history = signalRMethods.GetIoBrokerHistories(id, dt);
            serverToServerHubProxy.Invoke("SetIoBrokerHistories", callbackId, history);
        }

        private void GetAllDevices(short callbackId)
        {
            var devices = signalRMethods.GetAllDevices();
            serverToServerHubProxy.Invoke("SetAllDevices", callbackId, devices);
        }

        private void GetConfig(short callbackId, uint deviceId)
        {
            var conf = signalRMethods.GetConfig(deviceId);
            serverToServerHubProxy.Invoke("SetConfig", callbackId, conf);
        }

        internal void Update(Device device)
            => serverToServerHubProxy.Invoke("Update", device);

        public void Dispose()
        {
            hubConnection?.Dispose();
        }
    }
}
