using AppBroker.Core.Devices;

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace AppBroker.Core.Managers;

public interface IDeviceTypeMetaDataManager
{
    IReadOnlyCollection<PropertyInfo> Properties { get; }
    IReadOnlyCollection<string> PropertyNames { get; }
    IReadOnlyCollection<string> TypeNames { get; }
    IReadOnlyCollection<string> DeviceNames { get; }
    IReadOnlyCollection<long> DeviceIds { get; }
    ConcurrentDictionary<string, Type> AlternativeNamesForTypes { get; }

    Device? CreateDeviceFromName(string deviceName, Type? defaultType, params object[] ctorArgs);
    Device? CreateDeviceFromName(string deviceName, Type? defaultDevice, object[] ctorArgs, object[]? defaultDeviceCtorArgs);
    Device? CreateDeviceFromNameWithBaseType(string deviceName, Type? defaultType, Type baseType, params object[] ctorArgs);
    Device? CreateDeviceFromNameWithBaseType(string deviceName, Type baseType, Type? defaultDevice, object[] ctorArgs, object[]? defaultDeviceCtorArgs);
    void RegisterDeviceType(Type type);
}
