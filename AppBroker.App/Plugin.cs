
using AppBroker.App.Hubs;
using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;

using AppBrokerASP;
using AppBrokerASP.Extension;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using NLog;

using System.ComponentModel.DataAnnotations;

namespace AppBroker.App;

internal class Plugin : IPlugin
{
    public string Name => "App";


    public bool Initialize(LogFactory logFactory)
    {
        return true;
    }
}

internal class ServiceExtender : IServiceExtender
{

    public IEnumerable<Type> GetHubTypes()
    {
        yield return typeof(SmartHome);
        yield return typeof(HistoryHub);
        yield return typeof(DeviceHub);
        yield return typeof(LayoutHub);
    }
}
