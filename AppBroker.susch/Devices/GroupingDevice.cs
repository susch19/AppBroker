using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.HelperMethods;

using Newtonsoft.Json.Linq;

using NLog.Config;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.susch.Devices;

public enum GroupingMode
{
    Sum,
    Min,
    Max,
    Avg,

}
public class GroupingDevice<T> : Device where T : 
    INumber<T>, 
    IAdditionOperators<T, T, T>, 
    IDivisionOperators<T, T, T>, IMinMaxValue<T>
{
    public T Value => IInstanceContainer.Instance.DeviceStateManager.GetSingleState(Id, "something").ToObject<T>();

    private readonly Dictionary<long, T> storedStates = new();
    private readonly Dictionary<long, string> devices;
    private readonly string ownPropertyName;
    private readonly GroupingMode mode;

    public GroupingDevice(long nodeId, GroupingMode mode, string propName, params long[] ids) : base(nodeId)
    {
        this.mode = mode;
        ownPropertyName = propName;
        devices = ids.ToDictionary(x => x, _ => propName);
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
        
    }


    public GroupingDevice(long nodeId, GroupingMode mode, string propName, params (string name, long id)[] ids) : base(nodeId)
    {
        this.mode = mode;
        ownPropertyName = propName;
        devices = ids.ToDictionary(x => x.id, x => x.name);
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (!devices.TryGetValue(e.Id, out var propName) || e.PropertyName != propName)
            return;

        var value = e.NewValue.ToObject<T>();
        storedStates[e.Id] = value;

        if(storedStates.Count != devices.Count)
        {
            foreach (var item in devices)
            {
                if (storedStates.ContainsKey(item.Key))
                    continue;

                storedStates[item.Key] = IInstanceContainer.Instance.DeviceStateManager.GetSingleState(item.Key, item.Value).ToObject<T>();
            }
        }


        var newValue = mode switch
        {
            GroupingMode.Sum => storedStates.Values.Aggregate((x, y) => x + y),
            GroupingMode.Min => storedStates.Values.Min(),
            GroupingMode.Max => storedStates.Values.Max(),
            GroupingMode.Avg => storedStates.Values.Aggregate((x, y) => x + y) / GenericCaster<int, T>.Cast(storedStates.Count),
            _ => default
        };
        if (newValue != null)
        {
            IInstanceContainer.Instance.DeviceStateManager.PushNewState(Id, ownPropertyName, JToken.FromObject(newValue!));
            Console.WriteLine(newValue);
        }
    }
}
