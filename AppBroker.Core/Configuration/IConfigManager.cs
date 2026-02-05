using AppBroker.Plugins;
using AppBroker.Plugins.Configuration;

using Microsoft.Extensions.Configuration;

namespace AppBroker.Core.Configuration;

public interface IConfigManager
{
    IConfiguration Configuration { get; }
    HistoryConfig HistoryConfig { get; }
    MqttConfig MqttConfig { get; }
    ServerConfig ServerConfig { get; }
    DatabaseConfig DatabaseConfig { get; }
    IReadOnlyCollection<IConfig> PluginConfigs { get; }
}