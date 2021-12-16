using Elsa.Metadata;
using System.Reflection;
using AppBrokerASP;

namespace AppBroker.ValueProvider;

public class DeviceTypeNameProvider : IActivityPropertyOptionsProvider
{
    public object? GetOptions(PropertyInfo property) => IInstanceContainer.Instance.DevicePropertyManager.TypeNames;
}

