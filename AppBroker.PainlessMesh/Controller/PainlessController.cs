using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

using AppBrokerASP.Cloud;
using AppBrokerASP.Configuration;
using AppBrokerASP.Devices;

using Elsa.Server.Api.Attributes;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AppBroker.PainlessMesh;
[Route("painless")]
public class PainlessController : ControllerBase
{
    private readonly SmarthomeMeshManager? smarthomeManager;

    public PainlessController()
    {
        IInstanceContainer.Instance.TryGetDynamic(out smarthomeManager);
    }

    [HttpPut("time")]
    public ActionResult UpdateTime()
    {
        smarthomeManager?.UpdateTime();
        return smarthomeManager is null ? Problem("Mesh Manager not found") : Ok();
    }


}
