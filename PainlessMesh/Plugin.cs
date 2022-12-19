using AppBroker.Core;
using AppBroker.Core.Extension;

using AppBrokerASP;
using AppBrokerASP.Configuration;

using NLog;

using PainlessMesh.Ota;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PainlessMesh;
internal class Plugin : IPlugin
{
    public string Name { get; }

    

    public bool Initialize(LogFactory logFactory)
    {
        var cm = InstanceContainer.Instance.ConfigManager;
        var um = new UpdateManager();
        IInstanceContainer.Instance.RegisterDynamic(um);
        var mm = new SmarthomeMeshManager(cm.PainlessMeshConfig.Enabled, cm.PainlessMeshConfig.ListenPort);
        IInstanceContainer.Instance.RegisterDynamic(mm);

        if (InstanceContainer.Instance.ConfigManager.PainlessMeshConfig.Enabled)
            mm.Start(um);
        return true;
    }
}
