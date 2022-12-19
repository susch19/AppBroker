﻿using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

namespace AppBroker.Core;

public interface IInstanceContainer
{
    public static IInstanceContainer Instance { get; set; } = null!;
    IDeviceManager DeviceManager { get; }
    IDeviceTypeMetaDataManager DeviceTypeMetaDataManager { get; }
    IconService IconService { get; }
    IHistoryManager HistoryManager { get; }
    IDeviceStateManager DeviceStateManager { get; }
    JavaScriptEngineManager JavaScriptEngineManager { get; }

    void RegisterDynamic<T>(T instance) where T : class;
    bool TryGetDynamic<T>(out T? instance) where T : class;
}
