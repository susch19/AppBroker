

using AppBroker.Core;

using Elsa.Metadata;

using System.Reflection;

namespace AppBroker.ValueProvider;

public class DeviceIdProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DeviceTypeMetaDataManager.DeviceIds.Select(x => x.ToString("X2"));

}