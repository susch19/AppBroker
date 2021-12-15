using System.Collections.Immutable;
using System.Reflection;

namespace AppBrokerASP.Devices.Elsa;

public class DeviceTypeMetaDataManager : IDeviceTypeMetaDataManager
{
    public IReadOnlyCollection<PropertyInfo> Properties { get; }
    public IReadOnlyCollection<string> PropertyNames { get; }
    public IReadOnlyCollection<string> TypeNames { get; }

    public IReadOnlyCollection<long> DeviceIds => deviceIds;
    public IReadOnlyCollection<string> DeviceNames => deviceNames;

    private readonly List<long> deviceIds = new() {  };
    private readonly List<string> deviceNames = new() { "" };

    private readonly IDeviceManager manager;

    public DeviceTypeMetaDataManager(DeviceManager manager)
    {
        var stringArrWithEmpty = new[] { "" };
        this.manager = manager;
        Properties = manager.DeviceTypes.SelectMany(x => x.GetProperties()).Distinct().ToList();
        TypeNames = stringArrWithEmpty.Concat(manager.DeviceTypes.Select(x => x.Name).Distinct()).ToList();
        PropertyNames = stringArrWithEmpty.Concat(Properties.Select(x => x.Name).Distinct().OrderBy(x => x)).ToList();
        manager.NewDeviceAdded += Manager_NewDeviceAdded;
    }

    private void Manager_NewDeviceAdded(object? sender, (long id, AppBroker.Core.Devices.Device device) e)
    {
        if (!deviceIds.Contains(e.id))
             deviceIds.Add(e.id);
        if (!deviceNames.Contains(e.device.FriendlyName))
            deviceNames.Add(e.device.FriendlyName);
    }
}
