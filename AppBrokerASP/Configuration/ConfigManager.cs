namespace AppBrokerASP.Configuration;

public class ConfigManager
{
    public IConfiguration Configuration { get; }
    public ZigbeeConfig ZigbeeConfig { get; }
    public Zigbee2MqttConfig Zigbee2MqttConfig { get; }
    public PainlessMeshSettings PainlessMeshConfig { get; }
    public MqttConfig MqttConfig { get; }
    public ServerConfig ServerConfig { get; }
    public HistoryConfig HistoryConfig { get; }
    public CloudConfig CloudConfig { get; }

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

        PainlessMeshConfig = new PainlessMeshSettings();
        configuration.GetSection(PainlessMeshSettings.ConfigName).Bind(PainlessMeshConfig);

        ZigbeeConfig = new ZigbeeConfig();
        configuration.GetSection(ZigbeeConfig.ConfigName).Bind(ZigbeeConfig);

        Zigbee2MqttConfig = new Zigbee2MqttConfig();
        configuration.GetSection(Zigbee2MqttConfig.ConfigName).Bind(Zigbee2MqttConfig);

        MqttConfig = new MqttConfig();
        configuration.GetSection(MqttConfig.ConfigName).Bind(MqttConfig);

        ServerConfig = new ServerConfig();
        configuration.GetSection(ServerConfig.ConfigName).Bind(ServerConfig);

        CloudConfig = new CloudConfig();
        configuration.GetSection(CloudConfig.ConfigName).Bind(CloudConfig);

        HistoryConfig = new HistoryConfig();
        configuration.GetSection(HistoryConfig.ConfigName).Bind(HistoryConfig);
    }
}
