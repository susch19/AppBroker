
using AppBroker.Main;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

using System.Reflection;

namespace AppBroker.Runtime;

public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var pluginLoader = InstanceContainer.Instance.PluginLoader;

        foreach (var type in pluginLoader.ControllerTypes)
        {
            feature.Controllers.Add(type.GetTypeInfo());
        }
    }
}