using AppBroker.Core.Devices;
using AppBroker.Elsa.Signaler;

using AppBrokerASP.Extension;
using AppBrokerASP.IOBroker;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json;

using SocketIOClient;

using System.Collections.ObjectModel;
using System.Reflection;

using static AppBrokerASP.IOBroker.IoBrokerHistory;

namespace AppBrokerASP.Devices.Zigbee;

public abstract partial class ZigbeeDevice : WorkflowDevice<WorkflowPropertySignaler, WorkflowDeviceSignaler>
{
    protected SocketIO Socket { get; }

    //[property: JsonIgnore()]
    [ObservableProperty]
    private DateTime lastReceived;

    public string LastReceivedFormatted => lastReceived.ToString("dd.MM.yyyy HH:mm:ss");

    [ObservableProperty]
    [property: JsonProperty("link_Quality")]
    private byte linkQuality;

    [ObservableProperty]
    private bool available;

    [ObservableProperty]
    private string adapterWithId = "";

    private readonly ReadOnlyCollection<(string[] Names, PropertyInfo Info)> propertyInfos;

    public ZigbeeDevice(long nodeId, SocketIO socket) : base(nodeId)
    {
        Socket = socket;

        propertyInfos =
            Array.AsReadOnly(
                GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Concat(typeof(ZigbeeDevice).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Select(x =>
                {
                    var attr = x.GetCustomAttribute<JsonPropertyAttribute>();
                    return
                    (string.IsNullOrEmpty(attr?.PropertyName)
                    ? (new string[] { x.Name })
                    : (new string[] { x.Name, attr.PropertyName }),
                    x);
                })
            .ToArray());
    }

    public void SetPropFromIoBroker(IoBrokerObject ioBrokerObject, bool setLastReceived)
    {
        try
        {

            if (ioBrokerObject.ValueParameter.Value is null)
                return;

            var prop = propertyInfos.FirstOrDefault(x => x.Names.Any(y => ioBrokerObject.ValueName.Contains(y, StringComparison.OrdinalIgnoreCase))).Info;
            if (prop == default)
                return;

            SetProperty(ioBrokerObject, prop);
            if (setLastReceived)
                LastReceived = DateTime.Now;
            SendDataToAllSubscribers();

        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            Logger.Info(JsonConvert.SerializeObject(ioBrokerObject, Formatting.Indented));
        }
    }

    private void SetProperty(IoBrokerObject ioBrokerObject, PropertyInfo prop)
    {
        if ((prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double)) && ioBrokerObject.ValueParameter.Value.ToObject<string>()!.Contains(','))
        {
            var strValue = ioBrokerObject.ValueParameter.Value.ToObject<string>();
            if (double.TryParse(strValue, out var val))
                prop.SetValue(this, Convert.ChangeType(val, prop.PropertyType));
        }
        else if (prop.PropertyType == typeof(sbyte))
        {
            SetValueOnProperty<sbyte>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else if (prop.PropertyType == typeof(byte))
        {
            SetValueOnProperty<byte>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else if (prop.PropertyType == typeof(short))
        {
            SetValueOnProperty<short>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else if (prop.PropertyType == typeof(ushort))
        {
            SetValueOnProperty<ushort>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else if (prop.PropertyType == typeof(int))
        {
            SetValueOnProperty<int>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else if (prop.PropertyType == typeof(uint))
        {
            SetValueOnProperty<uint>(prop, ioBrokerObject.ValueParameter.Value.ToObject<long>());
        }
        else
        {
            prop.SetValue(this, Convert.ChangeType(ioBrokerObject.ValueParameter.Value.ToObject(prop.PropertyType), prop.PropertyType));
        }
    }

    private Dictionary<Type, MethodInfo> createSaturatingMethods = new();

    private void SetValueOnProperty<T>(PropertyInfo prop, long v1) // where T : INumber<T>, IMinMaxValue<T> 
    // => prop.SetValue(this, resT.CreateSaturating(v1));
    {
        if (!createSaturatingMethods.TryGetValue(typeof(T), out var method))
        {
            var methods = typeof(T).GetRuntimeMethods();
            var satMethod = methods.FirstOrDefault(x => x.Name.EndsWith("CreateSaturating"));
            method = satMethod!.MakeGenericMethod(typeof(T));
            createSaturatingMethods[typeof(T)] = method;
        }
        var res = method!.Invoke(null, new object[] { Convert.ChangeType(v1, typeof(T)) });
        prop.SetValue(this, res);
    }

    public async Task<List<IoBrokerHistory>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        var temp = GetHistory(start, end, HistoryType.Temperature);
        var humidity = GetHistory(start, end, HistoryType.Humidity);
        var pressure = GetHistory(start, end, HistoryType.Pressure);

        var result = new List<IoBrokerHistory>
            {
                await temp,
                await humidity,
                await pressure
            };
        return result;
    }

    public async Task<IoBrokerHistory> GetHistory(DateTimeOffset start, DateTimeOffset end, HistoryType type)
    {
        var history = new IoBrokerHistory(type.ToString().ToLower());

        var readFromFile = ReadHistoryJSON(start.Date, history);
        if (readFromFile is not null)
            return readFromFile;

        history.HistoryRecords = await GetHistoryRecords(start, end, type);
        return history;
    }

    public virtual IoBrokerHistory? ReadHistoryJSON(DateTime date, IoBrokerHistory history)
    {
        var filePath = Path.Combine(InstanceContainer.Instance.ConfigManager.ZigbeeConfig.HistoryPath, $"{date:yyyyMMdd}", $"history.{AdapterWithId}.{history.PropertyName}.json");
        if (File.Exists(filePath))
        {
            var records = Newtonsoft.Json.JsonConvert.DeserializeObject<HistoryRecord[]>(File.ReadAllText(filePath));
            history.HistoryRecords = records!;
            return history;
        }
        return null;
    }

    private async Task<HistoryRecord[]> GetHistoryRecords(DateTimeOffset start, DateTimeOffset end, HistoryType type)
    {
        var i = AdapterWithId + "." + type.ToString().ToLower();

        var endMs = end.ToUnixTimeMilliseconds();
        var history = await Socket.Emit("getHistory",
            i,
            new
            {
                id = i, // probably not necessary to put it here again
                start = start.ToUnixTimeMilliseconds(),
                //end = end.ToUnixTimeMilliseconds(),
                ignoreNull = true,
                aggregate = "none",
                count = 200
            });

        return history is null ? Array.Empty<HistoryRecord>() : history.GetValue<HistoryRecord[]>(1).Where(x => x.ts < endMs).ToArray();
    }
}

public enum HistoryType
{
    Temperature,
    Humidity,
    Pressure
}
