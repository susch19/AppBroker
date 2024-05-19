using AppBroker.Core;
using AppBroker.Core.Models;

using AppBrokerASP;
using AppBrokerASP.Devices;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AppBroker.IOBroker.Devices;

[AppBroker.ClassPropertyChangedAppbroker]
public partial class ZigbeeDevice : PropChangedJavaScriptDevice
{
    protected SocketIOClient.SocketIO Socket { get; }

    public bool Available
    {
        get => available;
        set
        {
            SetConnectionStatus(value);
            available = value;
        }
    }

    //[property: JsonIgnore()]
    private DateTime lastReceived;

    public string LastReceivedFormatted
    {
        get
        {
            var stateFromManager =
                IInstanceContainer.Instance.DeviceStateManager.GetSingleStateValue(Id, "lastReceived");
            if(stateFromManager is DateTime dt)
                lastReceived = dt;

            return lastReceived.ToString("dd.MM.yyyy HH:mm:ss");
        }
    }

    [JsonIgnore]
    public string AdapterWithId { get; set; }

    [AppBroker.IgnoreField]
    private readonly ReadOnlyCollection<(string[] Names, PropertyInfo Info)> propertyInfos;

    [AppBroker.IgnoreField]
    private readonly HashSet<string> unknownProperties = new();

    [AppBroker.IgnoreField]
    private bool available;

    public ZigbeeDevice(long nodeId, SocketIOClient.SocketIO socket, string typeName) : base(nodeId, typeName, new FileInfo(Path.Combine("JSExtensionDevices", typeName + ".js")))
    {
        TypeName = typeName;
        Socket = socket;
#if DEBUG
        StartAutomatically = true;
#endif
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

    public ZigbeeDevice(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, null, null)
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

            var valName = Regex.Replace(ioBrokerObject.ValueName, "_([a-z])", x => x.Value[1..].ToUpperInvariant());
            if (valName == "msgFromZigbee")
                return;
            var prop = propertyInfos.FirstOrDefault(x => x.Names.Any(y => valName.Equals(y, StringComparison.OrdinalIgnoreCase))).Info;
            SetState(valName, ioBrokerObject.ValueParameter.Value);

            if (ioBrokerObject?.ValueParameter?.Value is null)
                return;
            var hasProp = !unknownProperties.Contains(ioBrokerObject.ValueName);
            if (!hasProp && prop == default)
            {
                _ = unknownProperties.Add(ioBrokerObject.ValueName);
                return;
            }
            else if (hasProp && prop != default)
                SetProperty(ioBrokerObject, prop);
            if (setLastReceived)
            {
                LastReceived = DateTime.UtcNow;
                SetState("lastReceived", DateTime.UtcNow);
            }
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
        else if (prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double))
        {
            prop.SetValue(this, Convert.ChangeType(ioBrokerObject.ValueParameter.Value.ToObject(prop.PropertyType), prop.PropertyType));

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
        //else if (prop.PropertyType == typeof(sbyte))
        //{
        //    SetValueOnProperty<sbyte>(prop, ioBrokerObject.ValueParameter.Value.ToObject<sbyte>());
        //}
        //else if (prop.PropertyType == typeof(byte))
        //{
        //    SetValueOnProperty<byte>(prop, ioBrokerObject.ValueParameter.Value.ToObject<byte>());
        //}
        //else if (prop.PropertyType == typeof(short))
        //{
        //    SetValueOnProperty<short>(prop, ioBrokerObject.ValueParameter.Value.ToObject<short>());
        //}
        //else if (prop.PropertyType == typeof(ushort))
        //{
        //    SetValueOnProperty<ushort>(prop, ioBrokerObject.ValueParameter.Value.ToObject<ushort>());
        //}
        //else if (prop.PropertyType == typeof(int))
        //{
        //    SetValueOnProperty<int>(prop, ioBrokerObject.ValueParameter.Value.ToObject<int>());
        //}
        //else if (prop.PropertyType == typeof(uint))
        //{
        //    SetValueOnProperty<uint>(prop, ioBrokerObject.ValueParameter.Value.ToObject<uint>());
        //}
        else
        {
            prop.SetValue(this, Convert.ChangeType(ioBrokerObject.ValueParameter.Value.ToObject(prop.PropertyType), prop.PropertyType));
        }
    }

    //private void SetValueOnProperty<T>(PropertyInfo prop, T v1)  where T : INumber<T>, IMinMaxValue<T> 
    // => prop.SetValue(this, T.CreateSaturating(v1));

    [AppBroker.IgnoreField]
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


    public override async Task<List<History>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        var temp = GetHistory(start, end, HistoryType.Temperature.ToString());
        var humidity = GetHistory(start, end, HistoryType.Humidity.ToString());
        var pressure = GetHistory(start, end, HistoryType.Pressure.ToString());

        var result = new List<History>
            {
                await temp,
                await humidity,
                await pressure
            };
        return result;
    }

    public override async Task<History> GetHistory(DateTimeOffset start, DateTimeOffset end, string type)
    {

        if (InstanceContainer.Instance.ConfigManager.HistoryConfig.UseOwnHistoryManager)
            return await base.GetHistory(start, end, type);
        else
        {

            var history = new History(type.ToLower());
            var readFromFile = ReadHistoryJSON(start.Date, history);
            if (readFromFile is not null)
                return readFromFile;

            history.HistoryRecords = await GetHistoryRecords(start, end, type);
            return history;
        }
    }

    public virtual History? ReadHistoryJSON(DateTime date, History history)
    {
        var filePath= IInstanceContainer.Instance.GetDynamic<IoBrokerManager>().GetHistoryPath(date, AdapterWithId, history.PropertyName);

        if (File.Exists(filePath))
        {
            var records = JsonConvert.DeserializeObject<HistoryRecord[]>(File.ReadAllText(filePath));
            history.HistoryRecords = records!;
            return history;
        }
        return null;
    }

    private async Task<HistoryRecord[]> GetHistoryRecords(DateTimeOffset start, DateTimeOffset end, string type)
    {
        var i = AdapterWithId + "." + type.ToLower();

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
                count = 2000
            });

        return history is null ? Array.Empty<HistoryRecord>() : history.GetValue<HistoryRecord[]>(1).Where(x => x.Ts < endMs).ToArray();
    }
}

public enum HistoryType
{
    Temperature,
    Humidity,
    Pressure
}
