using AppBroker.Core.Devices;

using AppBrokerASP.Cloud;
using AppBrokerASP.Configuration;

using Microsoft.AspNetCore.Mvc;

using System.Security.Cryptography;
using System.Text;

namespace AppBrokerASP.Controllers;
[Route("device")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceStateManager stateManager;
    private readonly IDeviceManager deviceManager;

    public DeviceController(IDeviceStateManager stateManager, IDeviceManager deviceManager)
    {
        this.stateManager = stateManager;
        this.deviceManager = deviceManager;
    }

    [HttpGet("state/{id}")]
    public Dictionary<string, Newtonsoft.Json.Linq.JToken>? GetDeviceState([FromQuery]long id)
    {
        return stateManager.GetCurrentState(id);
    }

    [HttpGet("devices")]
    public ICollection<(long Id, string TypeName, string FriendlyName)> GetDevices()
    {
        return deviceManager.Devices.Values.Select(x=>(x.Id, x.TypeName, x.FriendlyName)).ToList();
    }

    [HttpGet("devices-stateful")]
    public ICollection<Device> GetDevicesStateful()
    {
        return deviceManager.Devices.Values;
    }



}
