using AppBroker.Core;

using AppBrokerASP.Devices.Elsa;


namespace AppBrokerASP;

public interface IInstanceContainer
{
    public static IInstanceContainer Instance { get; set; } = null!;
    IDeviceManager DeviceManager { get; }
    IDeviceTypeMetaDataManager DevicePropertyManager { get; }
    IconService IconService { get; }
}
