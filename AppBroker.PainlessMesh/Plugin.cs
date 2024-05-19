using AppBroker.Core;
using AppBroker.Core.Extension;
using AppBroker.PainlessMesh.Ota;

using AppBrokerASP;

using Microsoft.Extensions.Configuration;

using NLog;

namespace AppBroker.PainlessMesh;
internal class Plugin : IPlugin
{
    public string Name => "Plainless Mesh";

    
    public bool Initialize(LogFactory logFactory)
    {
        var cm = InstanceContainer.Instance.ConfigManager;
        var painlessMeshConfig = new PainlessMeshSettings();
        cm.Configuration.GetSection(PainlessMeshSettings.ConfigName).Bind(painlessMeshConfig);
        var um = new UpdateManager();
        IInstanceContainer.Instance.RegisterDynamic(um);

        var mm = new SmarthomeMeshManager(painlessMeshConfig.Enabled, painlessMeshConfig.ListenPort);
        IInstanceContainer.Instance.RegisterDynamic(mm);

        var mqttManager = new PainlessMeshMqttManager(painlessMeshConfig, mm);
        IInstanceContainer.Instance.RegisterDynamic(mqttManager);


        var pdm = new PainlessMeshDeviceManager();
        IInstanceContainer.Instance.RegisterDynamic(pdm);

        if (painlessMeshConfig.Enabled)
        {
            mqttManager.Connect().ContinueWith(_=> mqttManager.Subscribe());
            mm.Start(um);
        }
        return true;
    }
}
