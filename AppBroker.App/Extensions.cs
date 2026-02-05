using AppBroker.App.Controller;
using AppBroker.Core.DynamicUI;
using AppBroker.Core.Managers;

using AppBrokerASP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.App;
public static class Extensions
{
    public static DeviceLayout? GetLayout(this IDeviceManager deviceManager, string typeName, long deviceId)
    {
        DeviceLayout? layout = null;
        if (deviceId != 0)
            layout = DeviceLayoutService.GetDeviceLayout(deviceId)?.layout;
        if (layout is null && !string.IsNullOrWhiteSpace(typeName))
            layout = DeviceLayoutService.GetDeviceLayout(typeName)?.layout;
        if (layout is null && deviceManager.Devices.TryGetValue(deviceId, out var device))
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
}
