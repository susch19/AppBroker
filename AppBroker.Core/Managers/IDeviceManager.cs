using AppBroker.Core.Devices;

using System.Collections.Concurrent;

namespace AppBroker.Core.Managers;

public interface IDeviceManager
{
    ConcurrentDictionary<long, Device> Devices { get; }

    event EventHandler<(long id, Device device)>? NewDeviceAdded;

    bool AddNewDevice(Device device);
    void AddNewDevices(IReadOnlyCollection<Device> device);
    void LoadDevices();
    bool RemoveDevice(long id);
}
