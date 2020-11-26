using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Devices;
using AppBokerASP.Devices.Zigbee;
using AppBokerASP.IOBroker;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;

namespace AppBrokerASPCloud
{

    public class SmartHome : Hub
    {

        public static List<IClientProxy> ConnectedClients { get; internal set; } = new List<IClientProxy>();

        public static SmartHome Hub { get; private set; }

        public static ConcurrentDictionary<short, SmarthomeClientResponse> SmarthomeResultWaiter { get; }
        //HubConnection connection;

        static SmartHome()
        {
            SmarthomeResultWaiter = new ConcurrentDictionary<short, SmarthomeClientResponse>();
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

        public dynamic GetConfig(uint deviceId)
        {
            using var callback = new SmarthomeClientResponse<dynamic>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetConfig), callback.Id, deviceId));
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public List<Device> GetAllDevices()
        {
            using var callback = new SmarthomeClientResponse<List<Device>>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetAllDevices), callback.Id));
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public List<IoBrokerHistory> GetIoBrokerHistories(long id, string dt)
        {
            using var callback = new SmarthomeClientResponse<List<IoBrokerHistory>>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetIoBrokerHistories), callback.Id, id, dt));
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public IoBrokerHistory GetIoBrokerHistory(long id, string dt, string propertyName)
        {
            using var callback = new SmarthomeClientResponse<IoBrokerHistory>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetIoBrokerHistory), callback.Id, id, dt, propertyName));
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public List<IoBrokerHistory> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
        {

            using var callback = new SmarthomeClientResponse<List<IoBrokerHistory>>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetIoBrokerHistoriesRange), callback.Id, id, dt, dt2));
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public List<IoBrokerHistory> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
        {
            using var callback = new SmarthomeClientResponse<List<IoBrokerHistory>>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(GetIoBrokerHistoryRange), callback.Id, id, dt, dt2, propertyName));
            callback.ManualResetEvent.Wait();
            return callback.Result;

        }

        public List<Device> Subscribe(IEnumerable<long> DeviceIds)
        {
            return new List<Device>(); //TODO Has to be implemented
            using var callback = new SmarthomeClientResponse<List<Device>>();
            SmarthomeResultWaiter.TryAdd(callback.Id, callback);
            callback.ManualResetEvent.Wait();
            return callback.Result;
        }

        public void Update(GeneralSmarthomeMessage message) => ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(Update), message));
        public void UpdateDevice(long id, string newName) => ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(UpdateDevice), id, newName));
        public void SendUpdate(Device device) => ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(SendUpdate), device));
        public void UpdateTime() => ServerToServer.ConnectedClients.ForEach(x => x.SendAsync(nameof(UpdateTime)));


        public class SmarthomeClientResponse : IDisposable
        {
            private static short NextId => nextId++;
            private static volatile short nextId;


            public short Id { get; }
            public ManualResetEventSlim ManualResetEvent { get; }
            public SmarthomeClientResponse()
            {
                Id = NextId;
                ManualResetEvent = new ManualResetEventSlim(false);
            }

            public void SetResult()
            {
                ManualResetEvent.Set();
            }

            #region IDisposable Support
            protected bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                        ManualResetEvent.Dispose();
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }

        public class SmarthomeClientResponse<T> : SmarthomeClientResponse
        {
            public T Result { get; private set; }

            public SmarthomeClientResponse() : base()
            {

            }
            public void SetResult(T result)
            {
                Result = result;
                ManualResetEvent.Set();
            }

            protected override void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    base.Dispose(disposing);
                    if (disposing)
                        if (Result != null && Result is IDisposable disposable)
                            disposable.Dispose();
                    disposedValue = true;
                }
            }
        }
    }
}