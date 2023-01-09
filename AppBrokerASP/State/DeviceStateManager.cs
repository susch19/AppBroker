using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Core.Devices;

using Elsa.Activities.StateMachine;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace AppBrokerASP.State;

public class DeviceStateManager : IDeviceStateManager
{
    public event EventHandler<StateChangeArgs>? StateChanged;

    private readonly ConcurrentDictionary<long, Dictionary<string, JToken>> deviceStates = new();
    private readonly Dictionary<string, Dictionary<string, HashSet<string>>> propertyNameMappings = new();
    private readonly FileSystemWatcher fileSystemWatcher;
    private readonly Logger logger;

    public bool ManagesDevice(long id) => deviceStates.ContainsKey(id);

    public DeviceStateManager()
    {
        logger = LogManager.GetCurrentClassLogger();
        using var ctx = DbProvider.BrokerDbContext;
        foreach (var item in ctx.Devices.Where(x => !string.IsNullOrWhiteSpace(x.LastState)))
        {
            deviceStates[item.Id] = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(item.LastState!)!;
        }

        Directory.CreateDirectory("PropertyMappings");
        fileSystemWatcher = new FileSystemWatcher(new DirectoryInfo("PropertyMappings").FullName, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName |
                           NotifyFilters.LastWrite |
                           NotifyFilters.Security
        };
        fileSystemWatcher.Changed += FileChanged;
        fileSystemWatcher.Created += FileChanged;
        fileSystemWatcher.EnableRaisingEvents = true;
        foreach (var path in Directory.EnumerateFiles("PropertyMappings", "*.json", SearchOption.AllDirectories))
        {
            ReadPropertyMappings(path);
        }

    }


    //TODO Router or Mainspower get from Zigbee2Mqtt
    public Dictionary<string, JToken>? GetCurrentState(long id)
    {
        deviceStates.TryGetValue(id, out var result);
        return result;
    }
    public object? GetSingleStateValue(long id, string propertyName)
    {
        if (!deviceStates.TryGetValue(id, out var result))
            return null;
        if (!result.TryGetValue(propertyName, out var val) || val is null)
            return val;
        return val.Type switch
        {
            JTokenType.Integer => val.Value<long>(),
            JTokenType.Float => val.Value<double>(),
            JTokenType.String or JTokenType.Guid or JTokenType.Uri => val.Value<string>(),
            JTokenType.Boolean => val.Value<bool>(),
            JTokenType.Date => val.Value<DateTime>(),
            JTokenType.TimeSpan => val.Value<TimeSpan>(),
            _ => val.ToString(),
        };
    }

    public JToken? GetSingleState(long id, string propertyName)
    {
        if (!deviceStates.TryGetValue(id, out var result))
            return null;
        result.TryGetValue(propertyName, out var val);
        return val;
    }

    public bool TryGetCurrentState(long id, out Dictionary<string, JToken>? result)
        => deviceStates.TryGetValue(id, out result);

    /// <inheritdoc/>
    public void SetSingleState(long id, string propertyName, JToken newVal, StateFlags stateFlags = StateFlags.AllExceptThirdParty)
    {
        propertyName = MapValueName(id, propertyName);
        InstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device);

        if (deviceStates.TryGetValue(id, out var oldState))
        {

            if (oldState.ContainsKey(propertyName) && JToken.DeepEquals(oldState[propertyName], newVal))
                return;

            var oldVal = oldState.GetValueOrDefault(propertyName);
            InstanceContainer.Instance.HistoryManager.StoreNewState(id, propertyName, oldVal, newVal);

            AddStatesForBackwartsCompatibilityForOldApp(id, propertyName, newVal);
            oldState[propertyName] = newVal;
        }
        else
        {
            InstanceContainer.Instance.HistoryManager.StoreNewState(id, propertyName, null, newVal);
            AddStatesForBackwartsCompatibilityForOldApp(id, propertyName, newVal);

            deviceStates[id] = new Dictionary<string, JToken> { { propertyName, newVal } };
        }

        if ((stateFlags & StateFlags.NotifyOfStateChange) > 0)
            StateChanged?.Invoke(this, new(id, propertyName, null, newVal));

        if (device is not null && (stateFlags & StateFlags.SendToThirdParty) > 0)
            device.ReceivedNewState(propertyName, newVal, stateFlags);

        if (device is not null && (stateFlags & StateFlags.SendDataToApp) > 0)
            device.SendDataToAllSubscribers();

        if (device is not null && (stateFlags & StateFlags.StoreLastState) > 0)
            StoreLastState(id, deviceStates[id], device);
    }

    /// <inheritdoc/>
    public void SetMultipleStates(long id, Dictionary<string, JToken> newState, StateFlags stateFlags = StateFlags.AllExceptThirdParty)
    {
        foreach (var item in newState)
        {
            SetSingleState(id, item.Key, item.Value, stateFlags);
        }
    }

    private static void StoreLastState(long id, Dictionary<string, JToken> state, Device? dev)
    {
        if (dev is not null)
        {
            using var ctx = DbProvider.BrokerDbContext;

            var dbDev = ctx.Devices.FirstOrDefault(x => x.Id == id);
            if (dbDev is null)
            {
                dbDev = dev.GetModel();
                ctx.Devices.Add(dbDev);
            }
            dbDev.LastState = JsonConvert.SerializeObject(state);
            dbDev.LastStateChange = DateTime.UtcNow;
            ctx.SaveChanges();
        }
    }

    public void AddStatesForBackwartsCompatibilityForOldApp(long id, string name, JToken value)
    {
        string newPropName = name switch
        {
            "linkQuality" => "link_Quality",
            "transitionTime" => "transition_Time",
            "colorTemp" => "colortemp",
            _ => ""
        };
        if (string.IsNullOrWhiteSpace(newPropName))
            return;
        SetSingleState(id, newPropName, value);
    }



    private void FileChanged(object sender, FileSystemEventArgs e)
    {
        propertyNameMappings.Clear();
        foreach (var path in Directory.EnumerateFiles("PropertyMappings", "*.json", SearchOption.AllDirectories))
            ReadPropertyMappings(path);
    }

    private void ReadPropertyMappings(string path)
    {
        using var str = File.OpenRead(path);
        var res = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, HashSet<string>>>>(str);

        foreach (var (deviceType, mappings) in res)
        {
            ref var mapsForType = ref CollectionsMarshal.GetValueRefOrAddDefault(propertyNameMappings, deviceType, out _);
            mapsForType ??= new();

            logger.Trace($"Add Mapping for {deviceType}({mappings.Count})");
            foreach (var (propertyName, oldNames) in mappings)
            {
                mapsForType[propertyName] = oldNames;
            }
        }
    }

    public string MapValueName(long id, string name)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device))
        {
            foreach (var deviceType in device.TypeNames)
            {
                if (propertyNameMappings.TryGetValue(deviceType, out var mapsForType))
                {
                    foreach (var (newPropName, oldValueNames) in mapsForType)
                    {
                        if (oldValueNames.Contains(name))
                        {
                            return newPropName;
                        }
                    }
                }
            }
        }
        return name;
    }
}
