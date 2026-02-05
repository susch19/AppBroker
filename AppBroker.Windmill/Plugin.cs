using AppBroker.Core;
using AppBroker.Plugins.Extension;

using AppBrokerASP;

using Microsoft.Extensions.Configuration;

using NLog;

namespace AppBroker.Windmill;

internal class Plugin : IPlugin
{
    public string Name => "Windmill";
    public int LoadOrder => 0;

    public void RegisterTypes()
    {
    }

    public bool Initialize(LogFactory logFactory)
    {
        var forwarder = new StateForwarder(new HttpClient());
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += (_,e)=> forwarder.Forward(e);


        return true;
    }
}
