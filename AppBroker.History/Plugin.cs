
using AppBroker.Core;

using AppBroker.Plugins.Extension;

using NLog;

namespace AppBroker.History;

internal class Plugin : IPlugin
{
    public string Name => "History";

    public int LoadOrder => -110;
    public void RegisterTypes()
    {
        IInstanceContainer.Instance.RegisterDynamic(new HistoryManager());
    }

    public bool Initialize(LogFactory logFactory)
    {
        return true;
    }
}
