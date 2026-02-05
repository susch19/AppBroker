using AppBroker.Core;
using AppBroker.Main.Manager;
using AppBroker.Plugins.Extension;

using NLog;

namespace AppBroker.Main;

internal class Plugin : IPlugin
{
    public string Name => "Main";

    public int LoadOrder => -110;
    public void RegisterTypes()
    {
        IInstanceContainer.Instance.RegisterDynamic(new DeviceStateManager());
    }

    public bool Initialize(LogFactory logFactory)
    {
        return true;
    }
}
