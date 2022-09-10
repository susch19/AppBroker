using AppBroker.Core.Devices;
using AppBroker.Elsa.Signaler;

using AppBrokerASP.Extension;
using AppBrokerASP.IOBroker;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Newtonsoft.Json;

using SocketIOClient;

using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

using static AppBrokerASP.IOBroker.IoBrokerHistory;

namespace AppBrokerASP.Devices.Zigbee;

public partial class ZigbeeDevice : ConnectionJavaScriptDevice
{
    protected SocketIO Socket { get; }

    //[property: JsonIgnore()]
    private DateTime lastReceived;

    public string LastReceivedFormatted => lastReceived.ToString("dd.MM.yyyy HH:mm:ss");


    public System.DateTime LastReceived { get; set; }
    [JsonPropertyAttribute("link_Quality")]
    public byte LinkQuality { get; set; }

    public bool Available { get => Connected; set => Connected = value; }

    public string AdapterWithId { get; set; }

    [AppBroker.IgnoreField]
    private readonly ReadOnlyCollection<(string[] Names, PropertyInfo Info)> propertyInfos;

    [AppBroker.IgnoreField]
    private readonly HashSet<string> unknownProperties = new();

    public ZigbeeDevice(long nodeId, SocketIO socket, string typeName) : base(nodeId, typeName, new FileInfo(Path.Combine("JSExtensionDevices", typeName + ".js")))
    {
        TypeName = typeName;
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

    public ZigbeeDevice(long nodeId, SocketIO socket) : base(nodeId, null, null)
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
            if (unknownProperties.Contains(ioBrokerObject.ValueName))
                return;

            if (ioBrokerObject?.ValueParameter?.Value is null)
                return;
            var valName = ioBrokerObject.ValueName.Replace("_", "");
            var prop = propertyInfos.FirstOrDefault(x => x.Names.Any(y => valName.Equals(y, StringComparison.OrdinalIgnoreCase))).Info;
            SetState(valName, ioBrokerObject.ValueParameter.Value);
            if (prop == default)
            {
                Logger.Info($"Couldn't find proprty matching with {GetType().Name}.{valName}");
                _ = unknownProperties.Add(ioBrokerObject.ValueName);
                return;
            }

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
        else if ((prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double)))
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

    public virtual async Task<List<IoBrokerHistory>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        var temp = GetHistory(start, end, HistoryType.Temperature.ToString());
        var humidity = GetHistory(start, end, HistoryType.Humidity.ToString());
        var pressure = GetHistory(start, end, HistoryType.Pressure.ToString());

        var result = new List<IoBrokerHistory>
            {
                await temp,
                await humidity,
                await pressure
            };
        return result;
    }

    public virtual async Task<IoBrokerHistory> GetHistory(DateTimeOffset start, DateTimeOffset end, string type)
    {
        var history = new IoBrokerHistory(type.ToLower());

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

        return history is null ? Array.Empty<HistoryRecord>() : history.GetValue<HistoryRecord[]>(1).Where(x => x.ts < endMs).ToArray();
    }
}

public enum HistoryType
{
    Temperature,
    Humidity,
    Pressure
}
