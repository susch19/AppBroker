using AppBroker.Core.Models;
using AppBroker.Core;
using AppBroker.PainlessMesh;
using Newtonsoft.Json.Linq;

namespace AppBrokerASP.Devices.Painless;

public class Bridge : PainlessDevice
{
    public Bridge(long nodeId, List<string> parameter) : base(nodeId, "PainlessMeshBridge")
    {
        ShowInApp = false;
    }
}