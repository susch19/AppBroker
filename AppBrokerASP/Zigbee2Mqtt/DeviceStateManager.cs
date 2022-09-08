using Newtonsoft.Json.Linq;


namespace AppBrokerASP.Zigbee2Mqtt;

public class DeviceStateManager
{
    public event EventHandler<StateChangeArgs> StateChanged;

    private readonly ConcurrentDictionary<long, Dictionary<string, JToken>> deviceStates = new();

    public bool ManagesDevice(long id) => deviceStates.ContainsKey(id);

    //TODO Router or Mainspower get from Zigbee2Mqtt
    public Dictionary<string, JToken>? GetCurrentState(long id)
    {
        deviceStates.TryGetValue(id, out var result);
        return result;
    }

    public bool TryGetCurrentState(long id, out Dictionary<string, JToken>? result)
        => deviceStates.TryGetValue(id, out result);

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

            }
            deviceStates[id] = newState;
        }

        if (InstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device))
            device.SendDataToAllSubscribers();
    }
}
