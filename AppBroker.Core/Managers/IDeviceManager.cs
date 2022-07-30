using AppBroker.Core.Devices;
using System.Collections.Concurrent;

namespace AppBrokerASP;

public interface IDeviceManager
{
    ConcurrentDictionary<long, Device> Devices { get; }
    IReadOnlyCollection<Type> DeviceTypes { get; }

    void AddNewDevice(Device device);
}
