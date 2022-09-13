using AppBroker.Core.Devices;
using System.Collections.Concurrent;

namespace AppBrokerASP;

public interface IDeviceManager
{
    ConcurrentDictionary<long, Device> Devices { get; }
    IReadOnlyCollection<Type> DeviceTypes { get; }

    bool AddNewDevice(Device device);
    bool RemoveDevice(long id);
}
