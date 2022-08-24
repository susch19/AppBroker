using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.Devices;

public abstract partial class Device : ObservableObject
{
    public IReadOnlyCollection<string> TypeNames { get; }

    [JsonIgnore]
    public List<Subscriber> Subscribers { get; } = new List<Subscriber>();

    [ObservableProperty]
    private long id;

    [ObservableProperty]
    private string typeName;

    [JsonIgnore]
    [ObservableProperty]
    private bool showInApp;

    [ObservableProperty]
    private string friendlyName;

    [ObservableProperty]
    private bool isConnected;

    [JsonIgnore]
    public bool Initialized { get; set; }

    [JsonIgnore]
    protected NLog.Logger Logger { get; set; }

    public Device(long nodeId)
    {
        Initialized = false;
        Id = nodeId;
        TypeName = GetType().Name;
        TypeNames = GetBaseTypeNames(GetType()).ToArray();
        IsConnected = true;
        Logger = NLog.LogManager.GetCurrentClassLogger();
        FriendlyName = "";
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

    public virtual async void SendLastData(ISmartHomeClient client) => await client.Update(this);
    public virtual void SendLastData(List<ISmartHomeClient> clients) => clients.ForEach(async x => await x.Update(this));


    private Task? dataSendTask;
    public void SendDataToAllSubscribers()
    {
        if (dataSendTask != null && !dataSendTask.IsCompleted)
            return;
        dataSendTask = Task.Run(async () =>
            {
                await Task.Delay(250); //Wait for multiple property changes from endpoint, like zigbee
                Subscribers.ForEach(x => SendLastData(x.SmarthomeClient));
            });
    }

    public virtual void StopDevice() => IsConnected = false;
    public virtual void Reconnect(ByteLengthList parameter) => IsConnected = true;
}