﻿using AppBroker.Core.Devices;

using AppBrokerASP.Cloud;
using AppBrokerASP.Configuration;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

    [HttpGet("states/{id}")]
    public ActionResult GetDeviceState(long id)
    {
        var curState = stateManager.GetCurrentState(id);
        if (curState == null)
            return Content("No states found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpPost("states/{id}")]
    public ActionResult SetDeviceState(long id, [FromBody] Dictionary<string, JsonElement> newState)
    {
        stateManager.PushNewState(id, newState.ToDictionary(x => x.Key, x => JToken.Parse(x.Value.GetRawText())));

        var curState = stateManager.GetCurrentState(id);
        if (curState == null)
            return Content("No states found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpGet("state/{id}/{name}")]
    public ActionResult GetDeviceState(long id, string name)
    {
        var curState = stateManager.GetSingleState(id, name);
        if (curState == null)
            return Content("No state found");
        return Content(JsonConvert.SerializeObject((value: curState, name: name)), "application/json");
    }

    [HttpPost("state/{id}")]
    public ActionResult SetDeviceState(long id, [FromBody] NewStateRecord newState)
    {
        stateManager.SetSingleState(id, newState.Name, JToken.Parse(newState.Value.GetRawText()));

        var curState = stateManager.GetSingleState(id, newState.Name);
        if (curState == null)
            return Content("No state found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpPut]
    public ActionResult CreateDevice([FromBody] LogicFreeDevice deviceCreate)
    {
        if (deviceManager.Devices.TryGetValue(deviceCreate.Id, out var existing))
        {
            existing.ShowInApp = deviceCreate.ShowInApp;
            existing.FriendlyName = deviceCreate.FriendlyName;
            existing.SendDataToAllSubscribers();
        }
        else
        {
            deviceManager.AddNewDevice(deviceCreate);
        }

        return GetDevices();
    }


    [HttpGet]
    public ActionResult GetDevices([FromQuery]bool includeState = false)
    {
        return includeState
            ? Content(JsonConvert.SerializeObject(deviceManager.Devices.Values), "application/json")
            : (ActionResult)Content(JsonConvert.SerializeObject(deviceManager.Devices.Values.Select(x => new DeviceResponse(x.Id, x.TypeName, x.FriendlyName))), "application/json");
    }


    public record struct NewStateRecord(string Name, JsonElement Value);
    public record struct DeviceResponse(long Id, string TypeName, string FriendlyName);
    public record struct DeviceCreate(long Id, string TypeName, string FriendlyName, bool ShowInApp);

    public class LogicFreeDevice : Device
    {

        public override long Id { get; set; }
        public override string TypeName { get; set; }
        public override bool ShowInApp { get; set; }
        public override string FriendlyName { get; set; }

        public LogicFreeDevice(long id, string typeName, bool showInApp, string friendlyName) : base(id, typeName)
        {
            TypeName = typeName;
            ShowInApp = showInApp;
            FriendlyName = friendlyName;
        }

    }
}