using AppBokerASP;
using AppBokerASP.Devices;
using AppBokerASP.IOBroker;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AppBrokerASPCloud.SmartHome;

namespace AppBrokerASPCloud
{
    public class ServerToServer : Hub
    {
        public static List<IClientProxy> ConnectedClients { get; internal set; } = new List<IClientProxy>();
        private readonly static Dictionary<IClientProxy, HashSet<long>> callerDeviceIds = new Dictionary<IClientProxy, HashSet<long>>();
        //HubConnection connection;

        public ServerToServer()
        {
            //ConnectedClients.ForEach(x=>x.SendCoreAsync()

            //connection = new HubConnectionBuilder()
            //   .WithUrl("http://localhost:53353/ChatHub")
            //   .Build();
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

        public void SetConfig(short id, dynamic config) => SetResultOnSmarthomeResults(id, config);

        public void SetAllDevices(short id, List<Device> devices) => SetResultOnSmarthomeResults(id, devices);

        public void SetIoBrokerHistories(short id, List<IoBrokerHistory> histories) => SetResultOnSmarthomeResults(id, histories);

        public void SetIoBrokerHistory(short id, IoBrokerHistory history) => SetResultOnSmarthomeResults(id, history);

        public void SetIoBrokerHistoriesRange(short id, List<IoBrokerHistory> histories) => SetResultOnSmarthomeResults(id, histories);

        public void SetIoBrokerHistoryRange(short id, List<IoBrokerHistory> histories) => SetResultOnSmarthomeResults(id, histories);

        public List<Device> Subscribe(short id, IEnumerable<long> DeviceIds)
        {

            Clients.Caller
            return new List<Device>(); //TODO Has to be implemented
            //using var callback = new SmarthomeClientResponse<List<Device>>();
            //Callacks.TryAdd(callback.Id, callback);
            //callback.ManualResetEvent.Wait();
            //return callback.Result;
        }

        private void SetResultOnSmarthomeResults<T>(short id, T value)
        {
            if (SmarthomeResultWaiter.TryRemove(id, out var callback) && callback is SmarthomeClientResponse<T> c)
                c.SetResult(value);
        }
    }
}
