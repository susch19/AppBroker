using Elsa.Metadata;
using System.Reflection;
using AppBroker.Core;

namespace AppBroker.ValueProvider;

public class DeviceTypeNameProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DeviceTypeMetaDataManager.TypeNames;
}

