using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using AppBroker.Elsa.Signaler;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Newtonsoft.Json.Linq;

using PainlessMesh;

namespace AppBrokerASP.Devices
{
    [AppBroker.ClassPropertyChangedAppbroker]
    public abstract partial class Device
    {
        public List<Subscriber> Subscribers { get; } = new List<Subscriber>();
        private long id;
        private string typeName;
        private bool showInApp;
        private string friendlyName;
        private bool isConnected;

        [AppBroker.IgnoreField]
        protected readonly NLog.Logger logger;

        public Device(long nodeId)
        {
            id = nodeId;
            typeName = GetType().Name;
            isConnected = true;
            logger = NLog.LogManager.GetCurrentClassLogger();
            friendlyName = "";
        }

        public virtual Task UpdateFromApp(Command command, List<JToken> parameters) => Task.CompletedTask;
        public virtual void OptionsFromApp(Command command, List<JToken> parameters) { }

        public virtual dynamic? GetConfig() { return null; }

        public virtual async void SendLastData(ISmartHomeClient client) => await client.Update(this);
        public virtual void SendLastData(List<ISmartHomeClient> clients) => clients.ForEach(async x => await x.Update(this));
        public virtual void SendDataToAllSubscribers() => Subscribers.ForEach(x => SendLastData(x.SmarthomeClient));

        public virtual void StopDevice() => IsConnected = false;
        public virtual void Reconnect(ByteLengthList parameter) => IsConnected = true;

        protected virtual void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "")
        {
            WorkflowPropertySignaler.PropertyChanged(value, field, propertyName!);
            field = value;
        }
    }
}