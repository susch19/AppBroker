
using AppBrokerASP;

using Elsa.Metadata;

using System.Reflection;

namespace AppBroker.ValueProvider;

public class DeviceNameProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DevicePropertyManager.DeviceNames;

}
