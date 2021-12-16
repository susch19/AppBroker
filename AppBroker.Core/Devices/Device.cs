using Newtonsoft.Json.Linq;

namespace AppBroker.Core.Devices;

public abstract class Device
{
    public List<Subscriber> Subscribers { get; } = new List<Subscriber>();

    public abstract long Id { get; set; }

    public abstract string TypeName { get; set; }

    public abstract bool ShowInApp { get; set; }

    public abstract string FriendlyName { get; set; }

    public abstract bool IsConnected { get; set; }


    public bool Initialized { get; set; }

    protected NLog.Logger Logger { get; set; }

    public Device(long nodeId)
    {
        Initialized = false;
        Id = nodeId;
        TypeName = GetType().Name;
        IsConnected = true;
        Logger = NLog.LogManager.GetCurrentClassLogger();
        FriendlyName = "";
    }

    public virtual Task UpdateFromApp(Command command, List<JToken> parameters) => Task.CompletedTask;
    public virtual void OptionsFromApp(Command command, List<JToken> parameters) { }

    public virtual dynamic? GetConfig() => null;

    public virtual async void SendLastData(ISmartHomeClient client) => await client.Update(this);
    public virtual void SendLastData(List<ISmartHomeClient> clients) => clients.ForEach(async x => await x.Update(this));
    public virtual void SendDataToAllSubscribers() => Subscribers.ForEach(x => SendLastData(x.SmarthomeClient));

    public virtual void StopDevice() => IsConnected = false;
    public virtual void Reconnect(ByteLengthList parameter) => IsConnected = true;

}