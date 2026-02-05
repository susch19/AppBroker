using AppBroker.Core;
using AppBroker.Core.Configuration;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;
using AppBroker.Main.Configuration;
using AppBroker.Main.Manager;
using AppBroker.Plugins.Extension;

namespace AppBroker.Main;

public class InstanceContainer : IInstanceContainer, IDisposable
{
    public static InstanceContainer Instance { get; private set; } = null!;
    public IDeviceTypeMetaDataManager DeviceTypeMetaDataManager { get; }
    public JavaScriptEngineManager JavaScriptEngineManager { get; }

    public IDeviceManager DeviceManager { get; }
    public IconService IconService { get; }
    public IDeviceStateManager DeviceStateManager { get; private set; }
    public IHistoryManager HistoryManager { get; private set; }

    public IConfigManager ConfigManager => ServerConfigManager;
    public ConfigManager ServerConfigManager { get; }
    public IPluginLoader PluginLoader { get; }

    private Dictionary<Type, object> dynamicObjects = new();

    public InstanceContainer(IPluginLoader pluginLoader)
    {
        IInstanceContainer.Instance = Instance = this;
        PluginLoader = pluginLoader;
        IconService = new IconService();
        ServerConfigManager = new ConfigManager();

        JavaScriptEngineManager = new JavaScriptEngineManager();
        var localDeviceManager = new DeviceManager();
        DeviceManager = localDeviceManager;
        localDeviceManager.LoadDevices();
        DeviceTypeMetaDataManager = new DeviceTypeMetaDataManager(localDeviceManager);
        
        RegisterDynamic(PluginLoader);
        RegisterDynamic(IconService);
        RegisterDynamic(ServerConfigManager);
        RegisterDynamic(JavaScriptEngineManager);
        RegisterDynamic(localDeviceManager);
        RegisterDynamic(DeviceTypeMetaDataManager);
    }


    public void RegisterDynamic<T>(T instance) where T : class
    {
        dynamicObjects[typeof(T)] = instance;

        switch (instance)
        {
            case IHistoryManager hm: HistoryManager = hm; break;
            case IDeviceStateManager dsm: DeviceStateManager = dsm; break;
            default: break;
        }
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

    public IReadOnlyList<object> GetAllRegistered()
    {
        return dynamicObjects.Values.ToArray();
    }

    public void Dispose()
    {
        if (DeviceManager is IDisposable disposable)
            disposable.Dispose();
    }
}
