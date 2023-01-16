using AppBroker.Core;
using AppBroker.Core.Extension;

using AppBrokerASP;

using Microsoft.Extensions.Configuration;

using NLog;

namespace AppBroker.Zigbee2Mqtt;

internal class Plugin : IPlugin
{
    public string Name => "Zigbee2MQTT";

    public void RegisterTypes()
    {

        var cm = InstanceContainer.Instance.ConfigManager;
        var zigbee2MqttConfig = new Zigbee2MqttConfig();
        cm.Configuration.GetSection(Zigbee2MqttConfig.ConfigName).Bind(zigbee2MqttConfig);

        var um = new Zigbee2MqttManager(zigbee2MqttConfig);
        IInstanceContainer.Instance.RegisterDynamic(um);
        IInstanceContainer.Instance.RegisterDynamic(zigbee2MqttConfig);
    }

    public bool Initialize(LogFactory logFactory)
    {
        var um = IInstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>();
        var zigbee2MqttConfig = IInstanceContainer.Instance.GetDynamic<Zigbee2MqttConfig>();
        if (zigbee2MqttConfig.Enabled)
        {
            _ = um.Connect().ContinueWith((x) => _ = um.Subscribe());
        }

        return true;
    }
}
