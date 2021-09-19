using Microsoft.Extensions.Configuration;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Configuration
{
    public class ConfigManager
    {
        private IConfigurationRoot configuration;

        public ZigbeeConfig ZigbeeConfig { get; }
        public PainlessMeshSettings PainlessMeshConfig { get; }
        public ServerConfig ServerConfig {  get; }


        public ConfigManager()
        {
            var configBuilder = new ConfigurationBuilder();
            var fileName = "appsettings.json";
#if DEBUG
            fileName = "appsettings.debug.json";
#endif

            configBuilder.AddJsonFile(fileName);
            var configuration = configBuilder.Build();

            this.configuration = configuration;
            PainlessMeshConfig = new PainlessMeshSettings();
            configuration.GetSection(PainlessMeshSettings.ConfigName).Bind(PainlessMeshConfig);

            ZigbeeConfig = new ZigbeeConfig();
            configuration.GetSection(ZigbeeConfig.ConfigName).Bind(ZigbeeConfig);
            ServerConfig = new ServerConfig();
            configuration.GetSection(ServerConfig.ConfigName).Bind(ServerConfig);

        }

    }
}
