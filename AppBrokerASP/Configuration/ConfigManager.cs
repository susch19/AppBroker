﻿using AppBroker.Core.Configuration;

using AppBrokerASP.Plugins;

namespace AppBrokerASP.Configuration;

public class ConfigManager : IConfigManager
{
    public IConfiguration Configuration { get; }

    public MqttConfig MqttConfig { get; }
    public ServerConfig ServerConfig { get; }
    public HistoryConfig HistoryConfig { get; }
    public CloudConfig CloudConfig { get; }
    public DatabaseConfig DatabaseConfig { get; }
    public IReadOnlyCollection<IConfig> PluginConfigs => pluginConfigs;

    private List<IConfig> pluginConfigs = new();

    private static readonly string ConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "appbroker");
    private const string ZigbeeConfigName = "zigbee.json";
    private const string Zigbee2MqttConfigName = "zigbee2mqtt.json";
    private const string NlogConfigName = "nlog.json";
    private const string MqttConfigName = "MQTTClient.json";

#if DEBUG
    private const string ConfigName = "appsettings.debug.json";
#else
    private const string ConfigName = "appsettings.json";
#endif

    public ConfigManager()
    {
        var info = new DirectoryInfo(ConfigFolder);
        info.Create();

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(ConfigName, false, true)
            .AddJsonFile(Path.Combine(info.FullName, ZigbeeConfigName), true, true)
            .AddJsonFile(Path.Combine(info.FullName, Zigbee2MqttConfigName), true)

            .AddJsonFile(Path.Combine(info.FullName, NlogConfigName), true, true)
            .AddJsonFile(Path.Combine(info.FullName, MqttConfigName), true, true)
            .Build();

        Configuration = configuration;

        MqttConfig = new MqttConfig();
        configuration.GetSection(MqttConfig.ConfigName).Bind(MqttConfig);

        ServerConfig = new ServerConfig();
        configuration.GetSection(ServerConfig.ConfigName).Bind(ServerConfig);

        CloudConfig = new CloudConfig();
        configuration.GetSection(CloudConfig.ConfigName).Bind(CloudConfig);

        HistoryConfig = new HistoryConfig();
        configuration.GetSection(HistoryConfig.ConfigName).Bind(HistoryConfig);

        DatabaseConfig = new DatabaseConfig();
        configuration.GetSection(DatabaseConfig.ConfigName).Bind(DatabaseConfig);

        foreach (var item in InstanceContainer.Instance.PluginLoader.Configs)
        {
            configuration.GetSection(item.Name).Bind(item);
            pluginConfigs.Add(item);
        }
    }
}
