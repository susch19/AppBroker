using AppBroker.Core;

using Microsoft.AspNetCore.Mvc;


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
