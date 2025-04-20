using Microsoft.Extensions.Configuration;

namespace AppBroker.Core.Configuration;

public interface IConfigManager
{
    IConfiguration Configuration { get; }
    HistoryConfig HistoryConfig { get; }
    MqttConfig MqttConfig { get; }
    DatabaseConfig DatabaseConfig { get; }
    IReadOnlyCollection<IConfig> PluginConfigs { get; }
}