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
public record IconResponse(SvgIcon Icon, string Name);
public record LayoutResponse(DeviceLayout? Layout, IconResponse? Icon, IconResponse[] additionalIcons);


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
        var additional = GetAdditionalIcons(layout);

        return new LayoutResponse(layout, icon, additional);
    }

    private IconResponse[] GetAdditionalIcons(DeviceLayout? layout)
    {
        var histIconNames = 
            (layout?.DetailDeviceLayout?.HistoryProperties?.Select(x => x?.IconName) ?? Array.Empty<string>())
            .Concat(layout?.DetailDeviceLayout?.TabInfos?.Select(x=>x.IconName) ?? Array.Empty<string>())
            .Distinct();
        var additional = Array.Empty<IconResponse>();
        if (histIconNames is not null)
        {
            additional = histIconNames
                .Where(x => x is not null)
                .Select(x => GetIcon(null, x))
                .Where(x => x is not null)
                .Select(x => x!)
                .ToArray();
        }

        return additional;
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
                response.Add(new LayoutResponse(layout, icon, GetAdditionalIcons(layout)));
        }
        return response;
    }

    [HttpGet("all")]
    public List<LayoutResponse> GetAll()
    {
        try
        {
            return DeviceLayoutService
            .GetAllLayouts()
                .Select(x => new LayoutResponse(x, GetIcon(null, x.IconName), GetAdditionalIcons(x)))
                .ToList();
        }
        catch (Exception)
        {
            throw;
        }
    }

    [HttpGet("iconByName")]
    public SvgIcon GetAllIcons(string name)
    {
        return iconService.GetIconByName(name);
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

    private IconResponse? GetIcon(LayoutRequest? request, string? iconName)
    {
        IconResponse? res = null;
        SvgIcon? icon = null;
        if (!string.IsNullOrWhiteSpace(iconName))
        {
            icon = iconService.GetIconByName(iconName);
            if (icon is not null)
                res = new IconResponse(icon, iconName);
        }
        if (request is not null)
        {
            if (icon is null && !string.IsNullOrWhiteSpace(request?.IconName))
            {
                icon = iconService.GetIconByName(request.IconName);
                if (icon is not null)
                    res = new IconResponse(icon, request!.IconName);
            }
            if (icon is null && !string.IsNullOrWhiteSpace(request?.TypeName))
            {
                icon = iconService.GetBestFitIcon(request.TypeName);
                if (icon is not null)
                    res = new IconResponse(icon, request!.TypeName);
            }
        }
        return res;
    }

    [HttpPatch]
    public void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
}
