﻿using AppBroker.Core.Devices;

using System.Collections.Concurrent;

namespace AppBroker.Core.Managers;

public interface IDeviceManager
{
    ConcurrentDictionary<long, Device> Devices { get; }
    IReadOnlyCollection<Type> DeviceTypes { get; }

    bool AddNewDevice(Device device);
    bool RemoveDevice(long id);
}
