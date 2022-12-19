using AppBroker.Core;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

using AppBrokerASP.Configuration;
using AppBrokerASP.Devices.Elsa;
using AppBrokerASP.Histories;
using AppBrokerASP.State;
using AppBrokerASP.Zigbee2Mqtt;

using PainlessMesh.Ota;

namespace AppBrokerASP;

public class InstanceContainer : IInstanceContainer, IDisposable
{
    public static InstanceContainer Instance { get; private set; } = null!;
    public IDeviceTypeMetaDataManager DevicePropertyManager { get; }
    public JavaScriptEngineManager JavaScriptEngineManager { get; }
    public SmarthomeMeshManager MeshManager { get; }
    public IUpdateManager UpdateManager { get; }
    public ConfigManager ConfigManager { get; }
    public IDeviceManager DeviceManager { get; }
    public IconService IconService { get; }
    public IDeviceStateManager DeviceStateManager { get; }
    public IHistoryManager HistoryManager { get; } 
    public IZigbee2MqttManager Zigbee2MqttManager { get; } 

    public InstanceContainer()
    {
        IInstanceContainer.Instance = Instance = this;
        IconService = new IconService();
        ConfigManager = new ConfigManager();
        UpdateManager = new UpdateManager();
        MeshManager = new SmarthomeMeshManager(ConfigManager.PainlessMeshConfig.Enabled, ConfigManager.PainlessMeshConfig.ListenPort);
        DeviceStateManager = new DeviceStateManager();

        JavaScriptEngineManager = new JavaScriptEngineManager();
        HistoryManager = new HistoryManager();
        var localDeviceManager = new DeviceManager();
        DeviceManager = localDeviceManager;
        localDeviceManager.LoadDevices();
        DevicePropertyManager = new DeviceTypeMetaDataManager(localDeviceManager);
        Zigbee2MqttManager = new Zigbee2MqttManager(ConfigManager.Zigbee2MqttConfig);
    }

    public void Dispose()
    {
        if (DeviceManager is IDisposable disposable)
            disposable.Dispose();
        if (MeshManager is IDisposable disposable2)
            disposable2.Dispose();
    }
}
