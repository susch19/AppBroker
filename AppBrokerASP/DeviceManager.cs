using System.Reflection;
using System.Text;
using AppBrokerASP.IOBroker;

using PainlessMesh;
using AppBrokerASP.Configuration;
using AppBroker.Core;
using AppBroker.Core.Devices;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using AppBrokerASP.Zigbee2Mqtt;

using Device = AppBroker.Core.Devices.Device;
using AppBroker.Core.Managers;
using AppBroker.Core.Database;
using Newtonsoft.Json;

namespace AppBrokerASP;

public class DeviceManager : IDisposable, IDeviceManager
{
    public ZigbeeConfig Config { get; }
    public ConcurrentDictionary<long, Device> Devices { get; } = new();
    public IReadOnlyCollection<Type> DeviceTypes => types;

    public event EventHandler<(long id, Device device)>? NewDeviceAdded;

    private bool disposed;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly List<Type> types;
    private readonly Dictionary<string, Type> alternativeNamesForTypes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HttpClient http = new();
    private readonly IoBrokerManager? ioBrokerManager;

    public DeviceManager()
    {
        Config = InstanceContainer.Instance.ConfigManager.ZigbeeConfig;

        types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => typeof(Device).IsAssignableFrom(x) && x != typeof(Device))
            .ToList();

        foreach (var type in types)
        {
            var names = GetAllNamesFor(type);
            foreach (var name in names)
            {
                alternativeNamesForTypes[name] = type;
            }
        }

        InstanceContainer.Instance.MeshManager.NewConnectionEstablished += Node_NewConnectionEstablished;
        InstanceContainer.Instance.MeshManager.ConnectionLost += MeshManager_ConnectionLost;
        InstanceContainer.Instance.MeshManager.ConnectionReastablished += MeshManager_ConnectionReastablished;

        if (Config.Enabled is null or true)
        {
            ioBrokerManager = new IoBrokerManager(logger, http, Devices, alternativeNamesForTypes, Config);
            ioBrokerManager.NewDeviceAdded += NewDeviceAdded;
            _ = ioBrokerManager.ConnectToIOBroker();
        }

        if (InstanceContainer.Instance.ConfigManager.Zigbee2MqttConfig.Enabled)
        {
            var client = new Zigbee2MqttManager(InstanceContainer.Instance.ConfigManager.Zigbee2MqttConfig);
            _ = client.Connect().ContinueWith((x) => _ = client.Subscribe());
        }

        using var ctx = DbProvider.BrokerDbContext;
        foreach (var item in ctx.Devices.Where(x => x.StartAutomatically && x.DeserializationData != null))
        {
            var device = (Device)item.DeserializationData!.FromJsonTyped()!;
            var distinctNames = device.TypeNames.Distinct().ToArray();
            device.TypeNames.Clear();
            device.TypeNames.AddRange(distinctNames);

            AddNewDevice(device);
        }

    }
    public bool AddNewDevice(Device device)
    {
        if (Devices.TryAdd(device.Id, device))
        {
            NewDeviceAdded?.Invoke(this, (device.Id, device));
            device.StorePersistent();
            return true;
        }
        return false;
    }

    public bool RemoveDevice(long id)
    {
        if (Devices.Remove(id, out var device))
        {
            device.Dispose();
            return true;
        }
        return false;
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

    private void MeshManager_ConnectionLost(object? sender, uint e)
    {
        if (Devices.TryGetValue(e, out var device))
        {
            device.StopDevice();
        }
    }
    private void MeshManager_ConnectionReastablished(object? sender, (uint id, ByteLengthList parameter) e)
    {
        if (Devices.TryGetValue(e.id, out var device))
        {
            device.Reconnect(e.parameter);
        }
    }

    private void Node_NewConnectionEstablished(object? sender, (Sub c, ByteLengthList l) e)
    {
        if (Devices.TryGetValue(e.c.NodeId, out var device))
        {
            device.Reconnect(e.l);
        }
        else
        {
            var deviceName = Encoding.UTF8.GetString(e.l[1]);
            var newDevice = CreateDeviceFromName(deviceName, e.c.NodeId, e.l);

            if (newDevice is null)
                return;

            logger.Debug($"New Device: {newDevice.TypeName}, {newDevice.Id}");
            AddNewDevice(newDevice);
            //_ = Devices.TryAdd(e.c.NodeId, newDevice);

            if (!DbProvider.AddDeviceToDb(newDevice))
                _ = DbProvider.MergeDeviceWithDbData(newDevice);

        }
    }

    private Device? CreateDeviceFromName(string deviceName, params object[] ctorArgs)
    {
        logger.Trace($"Trying to get device with {deviceName} name");

        if (!alternativeNamesForTypes.TryGetValue(deviceName, out var type) || type is null)
        {
            logger.Error($"Failed to get device with name {deviceName}");
            return null;
        }

        var newDeviceObj = Activator.CreateInstance(type, ctorArgs);

        if (newDeviceObj is null || newDeviceObj is not Device newDevice)
        {
            logger.Error($"Failed to get create device {deviceName}");
            return null;
        }

        return newDevice;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                foreach (var device in Devices.Values.OfType<IDisposable>())
                {
                    device?.Dispose();
                }
                Devices.Clear();

                if (ioBrokerManager is not null)
                {
                    ioBrokerManager.NewDeviceAdded -= NewDeviceAdded;
                }
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}