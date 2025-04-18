using AppBroker.Core.DynamicUI;
using AppBroker.Core;
using AppBrokerASP;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.App.Hubs;

public partial class LayoutHub
{
    [Obsolete("Use REST Method instead")]
    public static string GetHashCodeByTypeName(string typeName) => InstanceContainer.Instance.IconService.GetBestFitIcon(typeName).Hash;
    [Obsolete("Use REST Method instead")]
    public static string GetHashCodeByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName).Hash;

    [Obsolete("Use REST Method instead")]
    public static SvgIcon GetIconByTypeName(string typename) => InstanceContainer.Instance.IconService.GetBestFitIcon(typename);
    [Obsolete("Use REST Method instead")]
    public static SvgIcon GetIconByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName);
    [Obsolete("Use REST Method instead")]
    public static SvgIcon GetIconByDeviceId(long deviceId) => InstanceContainer.Instance.IconService.GetBestFitIcon(InstanceContainer.Instance.DeviceManager.Devices[deviceId].TypeName);

    [Obsolete("Use REST Method instead")]
    public static void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
    [Obsolete("Use REST Method instead")]
    public static DeviceLayout? GetDeviceLayoutByName(string typename) => DeviceLayoutService.GetDeviceLayout(typename)?.layout;
    [Obsolete("Use REST Method instead")]
    public static DeviceLayout? GetDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDeviceLayout(id)?.layout;
    [Obsolete("Use REST Method instead")]
    public static List<DeviceLayout> GetAllDeviceLayouts() => DeviceLayoutService.GetAllLayouts();

    [Obsolete("Use REST Method instead")]
    public static LayoutNameWithHash? GetDeviceLayoutHashByDeviceId(long id)
    {
        var layoutHash = DeviceLayoutService.GetDeviceLayout(id);
        if (layoutHash is null || layoutHash.Value.layout is null)
            return null;

        return new(layoutHash.Value.layout.UniqueName, layoutHash.Value.hash);
    }
}
