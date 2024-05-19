using AppBroker.Core.Devices;
using AppBroker.Core.Managers;
using AppBroker.Core;
using System.Reflection;
using NLog;

namespace AppBrokerASP.Manager;


public class DeviceTypeMetaDataManager : IDeviceTypeMetaDataManager
{
    public IReadOnlyCollection<PropertyInfo> Properties => properties;
    public IReadOnlyCollection<string> PropertyNames => propertyNames;
    public IReadOnlyCollection<string> TypeNames => typeNames;

    public IReadOnlyCollection<long> DeviceIds => deviceIds;
    public IReadOnlyCollection<string> DeviceNames => deviceNames;
    public ConcurrentDictionary<string, Type> AlternativeNamesForTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    IReadOnlyCollection<Type> DeviceTypes => deviceTypes;

    private readonly HashSet<long> deviceIds = new() { };
    private readonly HashSet<string> deviceNames = new() { "" };
    private readonly HashSet<string> typeNames;
    private readonly HashSet<string> propertyNames;
    private readonly HashSet<PropertyInfo> properties;
    private readonly HashSet<Type> deviceTypes;

    private readonly NLog.ILogger logger;

    public DeviceTypeMetaDataManager(DeviceManager manager)
    {
        var stringArrWithEmpty = new[] { "" };
        logger = LogManager.GetCurrentClassLogger();
        deviceTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device))
        .ToHashSet();

        foreach (var type in deviceTypes)
        {
            var names = GetAllNamesFor(type);
            foreach (var name in names)
            {
                AlternativeNamesForTypes[name] = type;
            }
        }
        properties = deviceTypes.SelectMany(x => x.GetProperties()).ToHashSet();
        typeNames = stringArrWithEmpty.Concat(deviceTypes.Select(x => x.Name)).ToHashSet();
        propertyNames = stringArrWithEmpty.Concat(Properties.Select(x => x.Name).OrderBy(x => x)).ToHashSet();
        manager.NewDeviceAdded += Manager_NewDeviceAdded;
    }

    public void RegisterDeviceType(Type type)
    {
        if (!type.IsAssignableTo(typeof(Device)))
            return;

        deviceTypes.Add(type);
        typeNames.Add(type.Name);
        foreach (var name in GetAllNamesFor(type))
            AlternativeNamesForTypes[name] = type;

        foreach (var item in type.GetProperties())
        {
            properties.Add(item);
            propertyNames.Add(item.Name);
        }

    }

    private static List<string> GetAllNamesFor(Type y)
    {
        var names = new List<string>();
        var attribute = y.GetCustomAttribute<DeviceNameAttribute>();

        if (attribute is not null)
        {
            names.Add(attribute.PreferredName);
            names.AddRange(attribute.AlternativeNames);
        }
        names.Add(y.Name);
        return names;
    }

    public Device? CreateDeviceFromNameWithBaseType(string deviceName, Type baseType, Type? defaultDevice, params object[] ctorArgs)
        => CreateDeviceFromNameWithBaseType(deviceName, baseType, defaultDevice, ctorArgs, ctorArgs);
    public Device? CreateDeviceFromNameWithBaseType(string deviceName, Type baseType, Type? defaultDevice, object[] ctorArgs, object[]? defaultDeviceCtorArgs)
    {
        logger.Trace($"Trying to get device with {deviceName} name");
        try
        {

            if (!IInstanceContainer.Instance.DeviceTypeMetaDataManager.AlternativeNamesForTypes.TryGetValue(deviceName, out var type))
            {
                if (type is not null && !type.IsAssignableTo(baseType))
                {
                    if (defaultDevice is null)
                    {
                        logger.Warn($"Failed to get device with name {deviceName} and base device {baseType.Name} and no {nameof(defaultDevice)} type was passed.");

                        return null;
                    }

                }
                return (Device?)Activator.CreateInstance(defaultDevice, defaultDeviceCtorArgs);
            }

            var newDeviceObj = Activator.CreateInstance(type, ctorArgs);

            if (newDeviceObj is null || newDeviceObj is not Device newDevice)
            {
                logger.Error($"Failed to get create device {deviceName}");
                return null;
            }

            return newDevice;
        }
        catch (Exception ex)
        {

            logger.Error(ex, $"Couldn't create {deviceName} with default device {defaultDevice?.Name}");
            return null;
        }
    }

    public Device? CreateDeviceFromName(string deviceName, Type? defaultDevice, params object[] ctorArgs) => CreateDeviceFromName(deviceName, defaultDevice, ctorArgs, ctorArgs);

    public Device? CreateDeviceFromName(string deviceName, Type? defaultDevice, object[] ctorArgs, object[]? defaultDeviceCtorArgs)
    {
        try
        {

            logger.Trace($"Trying to get device with {deviceName} name");

            if (!IInstanceContainer.Instance.DeviceTypeMetaDataManager.AlternativeNamesForTypes.TryGetValue(deviceName, out var type))
            {
                if (defaultDevice is null)
                {
                    logger.Warn($"Failed to get device with name {deviceName} and no {nameof(defaultDevice)} type was passed.");
                    return null;
                }

                logger.Warn($"Failed to get device with name {deviceName}, using {defaultDevice.Name} instead");
                return (Device?)Activator.CreateInstance(defaultDevice, defaultDeviceCtorArgs);
            }

            var newDeviceObj = Activator.CreateInstance(type, ctorArgs);

            if (newDeviceObj is null || newDeviceObj is not Device newDevice)
            {
                logger.Error($"Failed to get create device {deviceName}");
                return null;
            }

            return newDevice;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Couldn't create {deviceName} with default device {defaultDevice?.Name}");
            return null;
        }
    }



    private void Manager_NewDeviceAdded(object? sender, (long id, AppBroker.Core.Devices.Device device) e)
    {
        if (!deviceIds.Contains(e.id))
            deviceIds.Add(e.id);
        if (!deviceNames.Contains(e.device.FriendlyName))
            deviceNames.Add(e.device.FriendlyName);
    }
}
