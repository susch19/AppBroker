﻿using AppBroker.Core;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

using AppBrokerASP.Configuration;
using AppBrokerASP.Devices.Elsa;
using AppBrokerASP.Histories;
using AppBrokerASP.State;
using AppBrokerASP.Zigbee2Mqtt;


namespace AppBrokerASP;

public class InstanceContainer : IInstanceContainer, IDisposable
{
    public static InstanceContainer Instance { get; private set; } = null!;
    public IDeviceTypeMetaDataManager DeviceTypeMetaDataManager { get; }
    public JavaScriptEngineManager JavaScriptEngineManager { get; }
    //public SmarthomeMeshManager MeshManager { get; }
    //public IUpdateManager UpdateManager { get; }
    public ConfigManager ConfigManager { get; }
    public IDeviceManager DeviceManager { get; }
    public IconService IconService { get; }
    public IDeviceStateManager DeviceStateManager { get; }
    public IHistoryManager HistoryManager { get; }
    public IZigbee2MqttManager Zigbee2MqttManager { get; }

    private Dictionary<Type, object> dynamicObjects = new();

    public InstanceContainer()
    {
        IInstanceContainer.Instance = Instance = this;
        IconService = new IconService();
        ConfigManager = new ConfigManager();
        DeviceStateManager = new DeviceStateManager();

        JavaScriptEngineManager = new JavaScriptEngineManager();
        HistoryManager = new HistoryManager();
        Zigbee2MqttManager = new Zigbee2MqttManager(ConfigManager.Zigbee2MqttConfig);
        var localDeviceManager = new DeviceManager();
        DeviceManager = localDeviceManager;
        localDeviceManager.LoadDevices();
        DeviceTypeMetaDataManager = new DeviceTypeMetaDataManager(localDeviceManager);
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



    public void Dispose()
    {
        if (DeviceManager is IDisposable disposable)
            disposable.Dispose();
    }
}
