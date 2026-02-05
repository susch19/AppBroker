using AppBroker.Plugins.Extension;

using NLog;

namespace AppBroker.Core;

internal class Plugin : IPlugin
{
    public string Name => "Core";

    public int LoadOrder => -10;
    public void RegisterTypes()
    {
    }

    public bool Initialize(LogFactory logFactory)
    {
        return true;
    }
}
