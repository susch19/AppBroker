using Newtonsoft.Json.Linq;


namespace AppBrokerASP.Zigbee2Mqtt;

public class DeviceStateManager : IDeviceStateManager
{
    public event EventHandler<StateChangeArgs>? StateChanged;

    private readonly ConcurrentDictionary<long, Dictionary<string, JToken>> deviceStates = new();

    public bool ManagesDevice(long id) => deviceStates.ContainsKey(id);

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

    public void SetSingleState(long id, string propertyName, JToken newVal)
    {

        if (deviceStates.TryGetValue(id, out var oldState))
        {
            bool changed = !oldState.ContainsKey(propertyName) || !JToken.DeepEquals(oldState[propertyName], newVal);

            if (!changed)
                return;

            var oldVal = oldState.GetValueOrDefault(propertyName);
            IInstanceContainer.Instance.HistoryManager.EnableHistory(id, propertyName);
            IInstanceContainer.Instance.HistoryManager.StoreNewState(id, propertyName, oldVal, newVal);
            StateChanged?.Invoke(this, new(id, propertyName, oldVal, newVal));
            AddStatesForBackwartsCompatibilityForOldApp(id, propertyName, newVal);
            oldState[propertyName] = newVal;
        }
        else
        {

            IInstanceContainer.Instance.HistoryManager.EnableHistory(id, propertyName); //TODO Don't enable all by default, rather provide a gui for setting it
            IInstanceContainer.Instance.HistoryManager.StoreNewState(id, propertyName, null, newVal);
            AddStatesForBackwartsCompatibilityForOldApp(id, propertyName, newVal);
            StateChanged?.Invoke(this, new(id, propertyName, null, newVal));

            deviceStates[id] = new Dictionary<string, JToken> { { propertyName, newVal } };
        }

        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device))
            device.SendDataToAllSubscribers();
    }

    public void PushNewState(long id, Dictionary<string, JToken> newState)
    {
        if (deviceStates.TryGetValue(id, out var oldState))
        {
            List<string> changedKeys = new();
            foreach (var item in newState.Keys)
            {
                if (!oldState.ContainsKey(item))
                    changedKeys.Add(item);
                else if (!JToken.DeepEquals(oldState[item], newState[item]))
                    changedKeys.Add(item);
            }
            if (changedKeys.Count == 0)
                return;
            foreach (var item in changedKeys)
            {
                var oldVal = oldState.GetValueOrDefault(item);
                var newVal = newState[item];
                InstanceContainer.Instance.HistoryManager.StoreNewState(id, item, oldVal, newVal);
                AddStatesForBackwartsCompatibilityForOldApp(id, item, newVal);
                StateChanged?.Invoke(this, new(id, item, oldVal, newVal));
                oldState[item] = newVal;
            }
        }
        else
        {

            foreach (var item in newState)
            {
                InstanceContainer.Instance.HistoryManager.EnableHistory(id, item.Key); //TODO Don't enable all by default, rather provide a gui for setting it
                InstanceContainer.Instance.HistoryManager.StoreNewState(id, item.Key, null, item.Value);
                AddStatesForBackwartsCompatibilityForOldApp(id, item.Key, item.Value);
            }
            deviceStates[id] = newState;
        }

        if (InstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device))
            device.SendDataToAllSubscribers();
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
}
