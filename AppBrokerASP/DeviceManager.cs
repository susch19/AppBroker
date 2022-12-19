using System.Reflection;
using System.Text;
using AppBrokerASP.IOBroker;

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

    public event EventHandler<(long id, Device device)>? NewDeviceAdded;

    private bool disposed;
    private readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private readonly List<Type> types;
    private static readonly HttpClient http = new();
    private readonly IoBrokerManager? ioBrokerManager;

    public DeviceManager()
    {
        Config = InstanceContainer.Instance.ConfigManager.ZigbeeConfig;


        if (Config.Enabled is null or true)
        {
            ioBrokerManager = new IoBrokerManager(logger, http, Devices, Config);
            ioBrokerManager.NewDeviceAdded += NewDeviceAdded;
            _ = ioBrokerManager.ConnectToIOBroker();
        }

        if (InstanceContainer.Instance.ConfigManager.Zigbee2MqttConfig.Enabled)
        {
            var client = InstanceContainer.Instance.Zigbee2MqttManager;
            _ = client.Connect().ContinueWith((x) => _ = client.Subscribe());
        }


    }

    public void LoadDevices()
    {
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

    public void AddNewDevices(IReadOnlyCollection<Device> devices)
    {
        foreach (var device in devices)
        {
            AddNewDevice(device);
        }
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