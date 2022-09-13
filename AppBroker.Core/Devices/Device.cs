using AppBroker.Core.DynamicUI;

using AppBrokerASP;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using NonSucking.Framework.Extension.Threading;

using System.Collections.Concurrent;

namespace AppBroker.Core.Devices;

public abstract class ConnectionDevice : Device
{

    public abstract bool IsConnected { get; set; }
    public ConnectionDevice(long nodeId, string typeName) : base(nodeId, typeName)
    {
        IsConnected = true;
    }
    protected ConnectionDevice(long nodeId) : base(nodeId)
    {
        IsConnected = true;
    }
    public override void StopDevice() => IsConnected = false;
    public override void Reconnect(ByteLengthList parameter) => IsConnected = true;
}

public abstract class Device : IDisposable
{
    public List<string> TypeNames { get; }

    [JsonIgnore]
    public HashSet<Subscriber> Subscribers { get; } = new HashSet<Subscriber>();

    public abstract long Id { get; set; }

    public abstract string TypeName { get; set; }

    [JsonIgnore]
    public abstract bool ShowInApp { get; set; }

    public abstract string FriendlyName { get; set; }

    [JsonIgnore]
    public bool Initialized { get; set; }

    [JsonIgnore]
    protected NLog.Logger Logger { get; set; }


    [JsonExtensionData]
    public Dictionary<string, JToken>? DynamicStateData => IInstanceContainer.Instance.DeviceStateManager.GetCurrentState(Id);
    private readonly Timer sendLastDataTimer;
    private readonly List<Subscriber> toRemove = new();

    public Device(long nodeId, string? typeName) : this(nodeId)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            TypeName = typeName;
            TypeNames.Insert(0, typeName);
        }
    }

    public Device(long nodeId)
    {
        Initialized = false;
        Id = nodeId;
        TypeName = GetType().Name;
        TypeNames = GetBaseTypeNames(GetType()).ToList();
        Logger = NLog.LogManager.GetCurrentClassLogger();
        FriendlyName = "";
        sendLastDataTimer = new Timer(async (s) =>
        {
            foreach (var item in Subscribers)
            {
                if (!await SendLastData(item))
                    toRemove.Add(item);

            }
            toRemove.ForEach(x => Subscribers.Remove(x));
            toRemove.Clear();
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    private IEnumerable<string> GetBaseTypeNames(Type type)
    {
        yield return type.Name;

        if (type.BaseType is null || type.BaseType == typeof(object))
            yield break;

        foreach (var item in GetBaseTypeNames(type.BaseType))
            yield return item;
    }

    public virtual Task UpdateFromApp(Command command, List<JToken> parameters) => Task.CompletedTask;
    public virtual void OptionsFromApp(Command command, List<JToken> parameters) { }

    public virtual dynamic? GetConfig() => null;

    public virtual async Task<bool> SendLastData(Subscriber client)
    {
        try
        {
            await client.SmarthomeClient.Update(this);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
        return true;
    }

    public virtual void SendLastData(List<ISmartHomeClient> clients) => clients.ForEach(async x => await x.Update(this));

    public void SendDataToAllSubscribers()
    {
        sendLastDataTimer.Change(250, Timeout.Infinite);
    }

    public virtual void StopDevice() { }
    public virtual void Reconnect(ByteLengthList parameter) { }

    public JToken? GetState(string name)
    {
        var currentState = DynamicStateData;
        return currentState is null || !currentState.TryGetValue(name, out var val) ? null : val;
    }

    public virtual void ReceivedNewState(string name, JToken newValue)
    {

    }
    public virtual void SetState(string name, JToken newValue)
        => IInstanceContainer.Instance.DeviceStateManager.SetSingleState(Id, name, newValue);

    public void Dispose()
    {
        sendLastDataTimer?.Dispose();
    }
}