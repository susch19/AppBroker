using AppBroker.Core.Devices;
using AppBroker.Core.Models;
using AppBroker.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AppBroker.Core.Managers;

namespace AppBroker.App.Controller;

public record struct SetHistoryRequest(bool Enable, List<long> Ids, string Name);

[Route("app/history")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryManager historyManager;
    private readonly IDeviceManager deviceManager;

    public HistoryController(IHistoryManager historyManager, IDeviceManager deviceManager)
    {
        this.historyManager = historyManager;
        this.deviceManager = deviceManager;
    }

    [HttpGet("settings")]
    public List<HistoryPropertyState> GetHistoryPropertySettings() => historyManager.GetHistoryProperties();


    [HttpPatch]
    public void SetHistories([FromBody] SetHistoryRequest request)
    {
        if (request.Enable)
        {
            foreach (var id in request.Ids)
                historyManager.EnableHistory(id, request.Name);
        }
        else
        {
            foreach (var id in request.Ids)
                historyManager.DisableHistory(id, request.Name);
        }
    }

    [HttpGet]
    public Task<List<History>> GetIoBrokerHistories([FromQuery] long id, [FromQuery] DateTime dt)
    {
        if (deviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return device.GetHistory(dt.Date, dt.Date.AddDays(1).AddSeconds(-1));
        }
        return Task.FromResult(new List<History>());
    }

    //Currently not used on the app
    //public Task<History> GetIoBrokerHistory(long id, string dt, string propertyName)
    //{
    //    if (deviceManager.Devices.TryGetValue(id, out Device? device))
    //    {
    //        DateTime date = DateTime.Parse(dt).Date;
    //        return device.GetHistory(date, date.AddDays(1).AddSeconds(-1), propertyName);
    //    }
    //    return Task.FromResult(History.Empty);
    //}


    //Currently not used on the app
    //public Task<List<History>> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
    //{
    //    if (deviceManager.Devices.TryGetValue(id, out Device? device))
    //    {
    //        return device.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2));
    //    }

    //    return Task.FromResult(new List<History>());
    //}

    [HttpGet("range")]
    public async Task<History> GetIoBrokerHistoryRange(long id, DateTime from, DateTime to, string propertyName)
    {
        if (deviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return await device.GetHistory(from, to, propertyName);
        }

        return History.Empty;
    }
}
