﻿using System.Reflection;

namespace AppBroker.Core.Managers;

public interface IDeviceTypeMetaDataManager
{
    IReadOnlyCollection<PropertyInfo> Properties { get; }
    IReadOnlyCollection<string> PropertyNames { get; }
    IReadOnlyCollection<string> TypeNames { get; }
    IReadOnlyCollection<string> DeviceNames { get; }
    IReadOnlyCollection<long> DeviceIds { get; }
}
