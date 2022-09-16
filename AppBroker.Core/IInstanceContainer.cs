using AppBroker.Core;

using AppBrokerASP.Devices.Elsa;
using AppBrokerASP.Javascript;

namespace AppBrokerASP;

public interface IInstanceContainer
{
    public static IInstanceContainer Instance { get; set; } = null!;
    IDeviceManager DeviceManager { get; }
    IDeviceTypeMetaDataManager DevicePropertyManager { get; }
    IconService IconService { get; }
    IHistoryManager HistoryManager { get; }
    IDeviceStateManager DeviceStateManager { get; }
    JavaScriptEngineManager JavaScriptEngineManager { get; }
}
