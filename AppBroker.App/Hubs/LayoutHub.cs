using AppBroker.Core.DynamicUI;
using AppBroker.Core;
using AppBrokerASP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.App.Hubs;
public class LayoutHub
{
    public record LayoutNameWithHash(string Name, string Hash);


    public static string GetHashCodeByTypeName(string typeName) => InstanceContainer.Instance.IconService.GetBestFitIcon(typeName).Hash;
    public static string GetHashCodeByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName).Hash;

    public static SvgIcon GetIconByTypeName(string typename) => InstanceContainer.Instance.IconService.GetBestFitIcon(typename);
    public static SvgIcon GetIconByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName);
    public static SvgIcon GetIconByDeviceId(long deviceId) => InstanceContainer.Instance.IconService.GetBestFitIcon(InstanceContainer.Instance.DeviceManager.Devices[deviceId].TypeName);

    public static void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
    public static DeviceLayout? GetDeviceLayoutByName(string typename) => DeviceLayoutService.GetDeviceLayout(typename)?.layout;
    public static DeviceLayout? GetDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDeviceLayout(id)?.layout;
    public static List<DeviceLayout> GetAllDeviceLayouts() => DeviceLayoutService.GetAllLayouts();

    public static LayoutNameWithHash? GetDeviceLayoutHashByDeviceId(long id)
    {
        var layoutHash = DeviceLayoutService.GetDeviceLayout(id);
        if (layoutHash is null || layoutHash.Value.layout is null)
            return null;

        return new(layoutHash.Value.layout.UniqueName, layoutHash.Value.hash);
    }
}
