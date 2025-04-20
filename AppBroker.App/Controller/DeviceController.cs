using AppBroker.Core.Devices;
using AppBroker.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBroker.App.Model;
using AppBroker.Core.Database;
using AppBroker.Core.Managers;
using Azure.Core;

namespace AppBroker.App.Controller;

public record struct DeviceRenameRequest(long Id, string NewName);

[Route("app/device")]
public class DeviceController : ControllerBase
{
    private readonly IDeviceManager deviceManager;

    public DeviceController(IDeviceManager deviceManager)
    {
        this.deviceManager = deviceManager;
    }

    [HttpGet]
    public List<Device> GetAllAppDevices()
    {
        var devices = deviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();
        var dev = JsonConvert.SerializeObject(devices);

        return devices;
    }

    [HttpGet("overview")]
    public List<DeviceOverview> GetDeviceOverview(bool onlyShowInApp = true) => deviceManager
        .Devices
        .Select(x => x.Value)
        .Where(x => !onlyShowInApp || x.ShowInApp)
        .Select(x => new DeviceOverview(x.Id, x.FriendlyName, x.TypeName, x.TypeNames))
        .ToList();

    [HttpPatch]
    public void UpdateDevice([FromBody] DeviceRenameRequest request)
    {
        if (deviceManager.Devices.TryGetValue(request.Id, out Device? stored))
        {
            stored.FriendlyName = request.NewName;
            _ = DbProvider.UpdateDeviceInDb(stored);
            stored.SendDataToAllSubscribers();
        }
    }

}
