using AppBroker.Core;

using AppBrokerASP.Configuration;
using AppBrokerASP.Devices.Elsa;

using PainlessMesh.Ota;

namespace AppBrokerASP;

public class InstanceContainer : IInstanceContainer, IDisposable
{
    public static InstanceContainer Instance { get; private set; } = null!;
    public IDeviceTypeMetaDataManager DevicePropertyManager { get; }
    public SmarthomeMeshManager MeshManager { get; }
    public IUpdateManager UpdateManager { get; }
    public ConfigManager ConfigManager { get; }
    public IDeviceManager DeviceManager { get; }
    public IconService IconService { get; }

    public InstanceContainer()
    {
        IInstanceContainer.Instance = Instance = this;
        IconService = new IconService();
        ConfigManager = new ConfigManager();
        UpdateManager = new UpdateManager();
        MeshManager = new SmarthomeMeshManager(ConfigManager.PainlessMeshConfig.Enabled, ConfigManager.PainlessMeshConfig.ListenPort);
        var localDeviceManager = new DeviceManager();

        DeviceManager = localDeviceManager;
        DevicePropertyManager = new DeviceTypeMetaDataManager(localDeviceManager);
    }

    public void Dispose()
    {
        if (DeviceManager is IDisposable disposable)
            disposable.Dispose();
        if (MeshManager is IDisposable disposable2)
            disposable2.Dispose();
    }
}
