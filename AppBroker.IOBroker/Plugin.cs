using AppBroker.Core;
using AppBroker.Core.Extension;

using AppBrokerASP;

using Microsoft.Extensions.Configuration;

using NLog;


namespace AppBroker.IOBroker;
public class Plugin : IPlugin
{
    public string Name { get; }
    public int LoadOrder => int.MinValue;

    public bool Initialize(LogFactory logFactory)
    {
        var cm = InstanceContainer.Instance.ConfigManager;

        var config = new ZigbeeConfig();
        cm.Configuration.GetSection(ZigbeeConfig.ConfigName).Bind(config);
        if (config.Enabled is null or true)
        {
            var ioBrokerManager = new IoBrokerManager(logFactory.GetLogger(nameof(IoBrokerManager)), config);
            _ = ioBrokerManager.ConnectToIOBroker();
            IInstanceContainer.Instance.RegisterDynamic(ioBrokerManager);
        }

        return true;
    }
}
