using AppBroker.Core.Database.Model;
using AppBroker.Core.Database;
using AppBroker.Core.DynamicUI;
using AppBroker.Core.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using NonSucking.Framework.Extension.Threading;

using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Runtime.CompilerServices;

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

    public virtual long Id { get; set; }

    public virtual string TypeName { get; set; }

    [JsonIgnore]
    public virtual bool ShowInApp { get; set; }

    public virtual string FriendlyName { get; set; }

    [JsonIgnore]
    public bool Initialized { get; set; }

    [JsonIgnore]
    protected NLog.Logger Logger { get; set; }

    public bool StartAutomatically { get; set; } = false;


    [JsonExtensionData]
    public Dictionary<string, JToken>? DynamicStateData => IInstanceContainer.Instance.DeviceStateManager.GetCurrentState(Id);
    private readonly Timer sendLastDataTimer;
    private readonly List<Subscriber> toRemove = new();


    public Device(long nodeId, string? typeName) : this(nodeId)
    {
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            TypeName = typeName;
            if (TypeNames.FirstOrDefault() != typeName)
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
        sendLastDataTimer = new Timer(async (s) => await SendLastDataTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
    }

    protected virtual async Task SendLastDataTimerElapsed()
    {
        foreach (var item in Subscribers)
        {
            if (!await SendLastData(item))
                toRemove.Add(item);

        }
        toRemove.ForEach(x => Subscribers.Remove(x));
        toRemove.Clear();
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

    public virtual void OptionsFromApp(Command command, List<JToken> parameters)
    {
        if (parameters.Count > 2 && parameters[0].ToObject<string>() == "store")
        {
            IOrderedEnumerable<HeaterConfig> hc = parameters.Skip(1).Select(x => x.ToDeObject<HeaterConfig>()).OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay);

            var models = hc.Select(x => (HeaterConfigModel)x).ToList();
            using BrokerDbContext? cont = DbProvider.BrokerDbContext;
            DeviceModel? d = cont.Devices.FirstOrDefault(x => x.Id == Id);
            if (d is not null)
            {
                foreach (HeaterConfigModel? item in models)
                    item.Device = d;
            }

            var oldConfs
                = cont
                    .HeaterConfigs
                    .Include(x => x.Device)
                    .Where(x => x.Device == null || x.Device.Id == Id)
                    .ToList();

            if (oldConfs.Count > 0)
            {
                cont.RemoveRange(oldConfs);
                _ = cont.SaveChanges();
            }
            cont.AddRange(models);
            _ = cont.SaveChanges();

        }

    }

    public virtual dynamic? GetConfig()
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        var configs = cont
            .HeaterConfigs
            .Where(x => x.DeviceId == Id)
            .ToList()
            .Select<HeaterConfigModel, HeaterConfig>(x => x)
            .ToList();
        if (configs.Count > 0)
            return configs.ToJson();

        return null;
    }

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

    public virtual void SendDataToAllSubscribers()
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

    /// <summary>
    /// Used for sending a state to the third party application. This will most likely be triggered from the <see cref="IDeviceStateManager"/>, but can also be called directly. This is not supposed to set the state in the <see cref="IDeviceStateManager"/>!
    /// </summary>
    /// <param name="name">the name of the property that was set</param>
    /// <param name="newValue">The value to send</param>
    public virtual void ReceivedNewState(string name, JToken newValue, StateFlags stateFlags)
    {

    }

    public virtual void SetState(string name, JToken newValue)
        => IInstanceContainer.Instance.DeviceStateManager.SetSingleState(Id, name, newValue);


    public virtual async Task<History> GetHistory(DateTimeOffset start, DateTimeOffset end, string type)
    {

        var history = new History(type);
        history.HistoryRecords = IInstanceContainer.Instance.HistoryManager.GetHistoryFor(Id, type, start.DateTime, end.DateTime);

        return history;

    }
    public virtual Task<List<History>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        return Task.FromResult(new List<History>());
    }

    public void StorePersistent()
    {
        using var ctx = DbProvider.BrokerDbContext;
        var existing = ctx.Devices.FirstOrDefault(x => x.Id == Id);
        if (existing is null)
        {
            existing = this.GetModel();
            ctx.Devices.Add(existing);
        }
        else
        {
            existing.StartAutomatically = StartAutomatically;
            existing.TypeName = TypeName;
            existing.FriendlyName = FriendlyName;

        }

        if (existing.StartAutomatically)
        {
            existing.DeserializationData = this.ToJsonTyped();
        }

        ctx.SaveChanges();
    }
    public void Dispose()
    {
        sendLastDataTimer?.Dispose();
    }

}