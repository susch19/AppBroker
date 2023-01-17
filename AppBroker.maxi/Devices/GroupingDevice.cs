using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.HelperMethods;

using Newtonsoft.Json.Linq;

using NLog;

using System.Numerics;

namespace AppBroker.maxi.Devices;

public class GroupingDevice<T> : Device where T :
    notnull,
    INumber<T>,
    IAdditionOperators<T, T, T>,
    IDivisionOperators<T, T, T>,
    IMinMaxValue<T>
{
    public T Value
    {

        get
        {
            var storedState = GetStoredState();

            if (!Equals(storedState, valueTemp))
            {
                logger.Warn($"State missmatch in Group Device {storedState} != {valueTemp}");
                logger.Info($"Try to push current state from: {storedState} => {valueTemp}");
                IInstanceContainer.Instance.DeviceStateManager.PushNewState(Id, ownPropertyName, JToken.FromObject(valueTemp));
            }


            return valueTemp;
        }
        private set
        {
            if (Equals(value, valueTemp))
                return;

            valueTemp = value;

            if (valueTemp != null)
            {
                IInstanceContainer.Instance.DeviceStateManager.PushNewState(Id, ownPropertyName, JToken.FromObject(valueTemp));
            }

            ValueChanged?.Invoke(this, valueTemp!);
        }
    }

    //Temporary to check how good storing works
    private T valueTemp;

    public event EventHandler<T>? ValueChanged;

    private readonly Dictionary<long, T> storedStates = new();
    private readonly Dictionary<long, string> devices;
    private readonly T defaultValue;
    private readonly string ownPropertyName;
    private readonly Logger logger;
    private readonly GroupingMode mode;

    public GroupingDevice(long nodeId, GroupingMode mode, string propName, T defaultValue, params long[] ids)
        : this(
              nodeId,
              mode,
              propName,
              ids.ToDictionary(x => x, _ => propName),
              defaultValue
        )
    {
    }

    public GroupingDevice(long nodeId, GroupingMode mode, string propName, T defaultValue, params (string name, long id)[] ids)
        : this(
              nodeId,
              mode,
              propName,
              ids.ToDictionary(x => x.id, x => x.name),
              defaultValue
        )
    {
    }

    private GroupingDevice(long nodeId, GroupingMode mode, string propName, Dictionary<long, string> devices, T defaultValue) : base(nodeId)
    {
        logger = LogManager.GetCurrentClassLogger();
        this.mode = mode;
        ownPropertyName = propName;
        ShowInApp = true;
        valueTemp = GetStoredState();
        this.devices = devices;
        this.defaultValue = defaultValue;
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (!devices.TryGetValue(e.Id, out var propName) || e.PropertyName != propName)
            return;

        logger.Debug("Group device rcv state change");

        var value = e.NewValue.ToObject<T>() ?? defaultValue;
        storedStates[e.Id] = value;

        if (storedStates.Count != devices.Count)
        {
            foreach (var item in devices)
            {
                if (storedStates.ContainsKey(item.Key))
                    continue;

                var state = IInstanceContainer.Instance.DeviceStateManager.GetSingleState(item.Key, item.Value);

                if (state is null)
                    continue;

                storedStates[item.Key] = state.ToObject<T>() ?? defaultValue;
            }
        }

        Value = mode switch
        {
            GroupingMode.Sum => storedStates.Values.Aggregate((x, y) => x + y),
            GroupingMode.Min => storedStates.Values.Min(),
            GroupingMode.Max => storedStates.Values.Max(),
            GroupingMode.Avg => storedStates.Values.Aggregate((x, y) => x + y) / GenericCaster<int, T>.Cast(storedStates.Count),
            _ => default
        } ?? defaultValue;
    }

    private T GetStoredState()
    {
        var state = IInstanceContainer.Instance.DeviceStateManager.GetSingleState(Id, ownPropertyName);

        if (state is null)
            return defaultValue;

        return state.ToObject<T>() ?? defaultValue;
    }
}
