

using AppBrokerASP;

using Elsa.Metadata;

using System.Reflection;

namespace AppBroker.ValueProvider;

public class DeviceIdProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DevicePropertyManager.DeviceIds.Select(x => x.ToString("X2"));

}