using AppBroker.Core;
using AppBroker.Core.Configuration;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

using AppBrokerASP.Configuration;
using AppBrokerASP.Histories;
using AppBrokerASP.State;

namespace AppBrokerASP;

public class InstanceContainer : IInstanceContainer, IDisposable
{
    public static InstanceContainer Instance { get; private set; } = null!;
    public IDeviceTypeMetaDataManager DeviceTypeMetaDataManager { get; }
    public JavaScriptEngineManager JavaScriptEngineManager { get; }

    public IDeviceManager DeviceManager { get; }
    public IconService IconService { get; }
    public IDeviceStateManager DeviceStateManager { get; }
    public IHistoryManager HistoryManager { get; }

    public IConfigManager ConfigManager => ServerConfigManager;
    public ConfigManager ServerConfigManager { get; }

    private Dictionary<Type, object> dynamicObjects = new();

    public InstanceContainer()
    {
        IInstanceContainer.Instance = Instance = this;
        IconService = new IconService();
        ServerConfigManager = new ConfigManager();
        DeviceStateManager = new DeviceStateManager();

        JavaScriptEngineManager = new JavaScriptEngineManager();
        HistoryManager = new HistoryManager();
        var localDeviceManager = new DeviceManager();
        DeviceManager = localDeviceManager;
        localDeviceManager.LoadDevices();
        //DeviceTypeMetaDataManager = new DeviceTypeMetaDataManager(localDeviceManager);
    }


    public void RegisterDynamic<T>(T instance) where T : class
    {
        dynamicObjects[typeof(T)] = instance;
    }
    public bool TryGetDynamic<T>(out T? instance) where T : class
    {
        var ret = dynamicObjects.TryGetValue(typeof(T), out var t);
        if (!ret)
        {
            instance = null;
            return ret;
        }

        instance = (T)t;
        return ret;
    }

    public T GetDynamic<T>() where T : class
    {
        return (T)dynamicObjects[typeof(T)];
    }

    public void Dispose()
    {
        if (DeviceManager is IDisposable disposable)
            disposable.Dispose();
    }
}
