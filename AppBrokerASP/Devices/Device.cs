using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PainlessMesh;

namespace AppBrokerASP.Devices
{
    public abstract class Device : IDisposable
    {
        public long Id { get; set; }
        public List<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
        public string TypeName { get; set; }
        public bool ShowInApp { get; set; }
        public string FriendlyName { get; set; }
        public bool IsConnected { get; set; }

        protected readonly NLog.Logger logger;
        protected bool Disposed;

        public Device(long nodeId)
        {
            Id = nodeId;
            TypeName = GetType().Name;
            IsConnected = true;
            logger = NLog.LogManager.GetCurrentClassLogger();
            FriendlyName = "";
        }

        public virtual Task UpdateFromApp(Command command, List<JToken> parameters) => Task.CompletedTask;
        public virtual void OptionsFromApp(Command command, List<JToken> parameters) { }

        public virtual dynamic? GetConfig() { return null; }

        public virtual async void SendLastData(ISmartHomeClient client) => await client.Update(this);
        public virtual void SendLastData(List<ISmartHomeClient> clients) => clients.ForEach(async x => await x.Update(this));
        public virtual void SendDataToAllSubscribers() => Subscribers.ForEach(x => SendLastData(x.SmarthomeClient));

        public virtual void StopDevice() => IsConnected = false;
        public virtual void Reconnect(ByteLengthList parameter) => IsConnected = true;

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                Disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}