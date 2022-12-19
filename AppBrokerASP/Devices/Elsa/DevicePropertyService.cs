using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Managers;


using Common.Logging;

using Jint.Runtime;

using SocketIOClient;

using System.Collections.Immutable;
using System.Reflection;

namespace AppBrokerASP.Devices.Elsa;

public class DeviceTypeMetaDataManager : IDeviceTypeMetaDataManager
{
    public IReadOnlyCollection<PropertyInfo> Properties => properties;
    public IReadOnlyCollection<string> PropertyNames => propertyNames;
    public IReadOnlyCollection<string> TypeNames => typeNames;

    public IReadOnlyCollection<long> DeviceIds => deviceIds;
    public IReadOnlyCollection<string> DeviceNames => deviceNames;
    public ConcurrentDictionary<string, Type> AlternativeNamesForTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    IReadOnlyCollection<Type> DeviceTypes => deviceTypes;

    private readonly List<long> deviceIds = new() { };
    private readonly List<string> deviceNames = new() { "" };
    private readonly List<string> typeNames ;
    private readonly List<string> propertyNames ;
    private readonly List<PropertyInfo> properties;

    private readonly IDeviceManager manager;
    private readonly ILog logger;
    private readonly List<Type> deviceTypes;

    public DeviceTypeMetaDataManager(DeviceManager manager)
    {
        var stringArrWithEmpty = new[] { "" };
        this.manager = manager;
        logger = LogManager.GetCurrentClassLogger();
        deviceTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device))
        .ToList();

        foreach (var type in deviceTypes)
        {
            var names = GetAllNamesFor(type);
            foreach (var name in names)
            {
                AlternativeNamesForTypes[name] = type;
            }
        }
        properties = deviceTypes.SelectMany(x => x.GetProperties()).Distinct().ToList();
        typeNames = stringArrWithEmpty.Concat(deviceTypes.Select(x => x.Name).Distinct()).ToList();
        propertyNames = stringArrWithEmpty.Concat(Properties.Select(x => x.Name).Distinct().OrderBy(x => x)).ToList();
        manager.NewDeviceAdded += Manager_NewDeviceAdded;
    }

    public void RegisterDeviceType(Type type)
    {
        if (!type.IsAssignableTo(typeof(AppBroker.Core.Devices.Device)))
            return;


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
            return (Device?)Activator.CreateInstance(defaultDevice, ctorArgs);
        }

        var newDeviceObj = Activator.CreateInstance(type, ctorArgs);

        if (newDeviceObj is null || newDeviceObj is not Device newDevice)
        {
            logger.Error($"Failed to get create device {deviceName}");
            return null;
        }

        return newDevice;
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
            logger.Error($"Couldn't create {deviceName} with default device {defaultDevice?.Name}", ex);
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
