using Microsoft.Extensions.Configuration;

namespace AppBrokerASP.Configuration
{
    public class ConfigManager
    {
        public ZigbeeConfig ZigbeeConfig { get; }
        public PainlessMeshSettings PainlessMeshConfig { get; }

        public ConfigManager()
        {
            var configBuilder = new ConfigurationBuilder();
            var fileName = "appsettings.json";
#if DEBUG
            fileName = "appsettings.debug.json";
#endif

            configBuilder.AddJsonFile(fileName);
            var configuration = configBuilder.Build();

            PainlessMeshConfig = new PainlessMeshSettings();
            configuration.GetSection(PainlessMeshSettings.ConfigName).Bind(PainlessMeshConfig);

            ZigbeeConfig = new ZigbeeConfig();
            configuration.GetSection(ZigbeeConfig.ConfigName).Bind(ZigbeeConfig);
        }
    }
}
