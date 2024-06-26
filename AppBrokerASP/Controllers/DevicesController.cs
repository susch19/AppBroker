﻿using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Javascript;
using AppBroker.Core.Managers;

using AppBrokerASP.Cloud;
using AppBrokerASP.Configuration;
using AppBrokerASP.Devices;

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
    private readonly JavaScriptEngineManager engineManager;
    private readonly IHistoryManager historyManager;

    public DeviceController(IDeviceStateManager stateManager, IDeviceManager deviceManager, JavaScriptEngineManager engineManager, IHistoryManager historyManager)
    {
        this.stateManager = stateManager;
        this.deviceManager = deviceManager;
        this.engineManager = engineManager;
        this.historyManager = historyManager;
    }

    [HttpPatch("rebuild/{id}")]
    public ActionResult ReloadJSDevices(long id)
    {
        if (deviceManager.Devices.TryGetValue(id, out var device) && device is JavaScriptDevice jsd)
        {
            jsd.RebuildEngine();
            return Content($"Successfully rebuild {id}");
        }
        return Content("Zigbee2MqttDeviceJson not found");
    }

    [HttpPatch]
    public ActionResult ReloadJSDevices([FromQuery] bool onlyNew = true)
    {
        engineManager.ReloadJsDevices(onlyNew);
        return GetDevices();
    }

    [HttpGet("states/{id}")]
    public ActionResult GetDeviceStates(long id)
    {
        var curState = stateManager.GetCurrentState(id);
        if (curState == null)
            return Content("No states found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpPost("states/{id}")]
    public async Task<ActionResult> SetDeviceStates(long id)
    {

        Dictionary<string, JToken> newStates;
        using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            newStates = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, JToken>>(await reader.ReadToEndAsync())!;
        }

        stateManager.SetMultipleStates(id, newStates);

        var curState = stateManager.GetCurrentState(id);
        if (curState == null)
            return Content("No states found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpGet("history/{id}")]
    public ActionResult GetHistory(long id, [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string propName)
    {
        return Content(JsonConvert.SerializeObject(historyManager.GetHistoryFor(id, propName, from, to)));
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
    public async Task<ActionResult> SetDeviceState(long id)
    {

        NewStateRecord newState;
        using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            newState = Newtonsoft.Json.JsonConvert.DeserializeObject<NewStateRecord>(await reader.ReadToEndAsync());
        }


        stateManager.SetSingleState(id, newState.Name, newState.Value);

        var curState = stateManager.GetSingleState(id, newState.Name);
        if (curState == null)
            return Content("No state found");
        return Content(JsonConvert.SerializeObject(curState), "application/json");
    }

    [HttpPut]
    public ActionResult CreateDevice([FromBody] RestCreatedDevice deviceCreate)
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
    public ActionResult GetDevices([FromQuery] bool includeState = false)
    {
        return includeState
            ? Content(JsonConvert.SerializeObject(deviceManager.Devices.Values), "application/json")
            : (ActionResult)Content(JsonConvert.SerializeObject(deviceManager.Devices.Values.Select(x => new DeviceResponse(x.Id, x.TypeName, x.FriendlyName))), "application/json");
    }


    public record struct NewStateRecord(string Name, JToken Value);
    public record struct DeviceResponse(long Id, string TypeName, string FriendlyName);


    public class RestCreatedDevice : JavaScriptDevice
    {

        [JsonConstructor]
        public RestCreatedDevice(long id, string typeName, bool showInApp, string friendlyName, bool autoStart = false) : base(id, typeName, new FileInfo(Path.Combine("JSExtensionDevices", typeName + ".js")))
        {
            ShowInApp = showInApp;
            FriendlyName = friendlyName;
            StartAutomatically = autoStart;
        }

    }
}
