using AppBroker.Core.Devices;

namespace AppBroker.Core;

public interface ISmartHomeClient
{
    Task Update(Device device);
}
