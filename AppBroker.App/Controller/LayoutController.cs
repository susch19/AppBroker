using AppBroker.Core.DynamicUI;
using AppBroker.Core;
using AppBrokerASP;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBroker.App.Hubs;
using AppBroker.Core.Managers;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using AppBroker.Core.Devices;

namespace AppBroker.App.Controller;

public record LayoutRequest(string TypeName, string IconName, long DeviceId);
public record LayoutResponse(DeviceLayout? Layout, SvgIcon? Icon);


[Route("app/layout")]
public class LayoutController : ControllerBase
{
    private readonly IconService iconService;
    private readonly IDeviceManager deviceManager;

    public LayoutController(IconService iconService, IDeviceManager deviceManager)
    {
        this.iconService = iconService;
        this.deviceManager = deviceManager;
    }

    [HttpGet("single")]
    public LayoutResponse GetSingle([FromQuery] LayoutRequest request)
    {
        var layout = GetLayout(request);
        var icon = GetIcon(request, layout?.IconName);

        return new LayoutResponse(layout, icon);
    }
    [HttpGet("multi")]
    public List<LayoutResponse> GetMultiple([FromQuery] List<LayoutRequest> request)
    {
        var response = new List<LayoutResponse>();

        foreach (var req in request)
        {
            var layout = GetLayout(req);
            var icon = GetIcon(req, layout?.IconName);
            if (icon is not null || layout is not null)
                response.Add(new LayoutResponse(layout, icon));
        }
        return response;
    }

    [HttpGet("all")]
    public List<LayoutResponse> GetAll()
    {
        return DeviceLayoutService
            .GetAllLayouts()
            .Select(x => new LayoutResponse(x, GetIcon(null, x.IconName)))
            .ToList();
    }

    private DeviceLayout? GetLayout(LayoutRequest request)
    {
        DeviceLayout? layout = null;
        if (request.DeviceId != 0)
            layout = DeviceLayoutService.GetDeviceLayout(request.DeviceId)?.layout;
        if (layout is null && !string.IsNullOrWhiteSpace(request.TypeName))
            layout = DeviceLayoutService.GetDeviceLayout(request.TypeName)?.layout;
        if (layout is null && deviceManager.Devices.TryGetValue(request.DeviceId, out var device))
        {
            foreach (var item in device.TypeNames)
            {
                if (DeviceLayoutService.GetDeviceLayout(item) is { } res && res.layout is { } resLayout)
                {
                    layout = resLayout;
                    break;
                }
            }
        }
        return layout;
    }

    private SvgIcon? GetIcon(LayoutRequest? request, string? iconName)
    {
        SvgIcon? icon = null;
        if (!string.IsNullOrWhiteSpace(iconName))
        {
            icon = iconService.GetIconByName(iconName);
        }
        if (request is not null)
        {
            if (icon is null && !string.IsNullOrWhiteSpace(request?.IconName))
            {
                icon = iconService.GetIconByName(request.IconName);
            }
            if (icon is null && !string.IsNullOrWhiteSpace(request?.TypeName))
            {
                icon = iconService.GetBestFitIcon(request.TypeName);
            }
        }
        return icon;
    }

    [HttpPatch]
    public void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
}
