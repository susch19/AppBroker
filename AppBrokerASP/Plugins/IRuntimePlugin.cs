using AppBroker.Plugins.Extension;

namespace AppBrokerASP.Plugins;

internal interface IRuntimePlugin : IPlugin
{
    void InitializeStartup(string[] args);
    void Run();
}
