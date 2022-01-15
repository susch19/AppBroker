using AppBrokerASP;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.DynamicUI;

public static class DeviceLayoutService
{
    public static Dictionary<string, DeviceLayout> TypeDeviceLayouts = new();
    public static Dictionary<long, DeviceLayout> InstanceDeviceLayouts = new();

    private static string DemoFilePath = Path.Combine("DeviceLayouts", "SampleLayout.json");
    private static readonly FileSystemWatcher fileSystemWatcher;

    static DeviceLayoutService()
    {
        fileSystemWatcher = new FileSystemWatcher(new DirectoryInfo("DeviceLayouts").FullName, "*.json");
        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
        fileSystemWatcher.Changed += FileChanged;
        fileSystemWatcher.Created += FileChanged;
        fileSystemWatcher.EnableRaisingEvents = true;

        if (!File.Exists(DemoFilePath))
        {
            var deviceLayout = new DeviceLayout("Demo",
                new() { -1, -2 },
                new(new()
                {
                    new("PropName", 0, TextStyle: new(22, "FontName", FontWeight.Bold, FontStyle.Italic))
                }),
                new(PropertyInfos: new()
                {
                    new("PropNameUpdateable", 0, EditInfo: new(MessageType.Update, Command.Delay), TextStyle: new(12, "FontName")),
                    new("PropNameNotUpdable", 1),
                    new("PropNameTempCurrent", 2, TextStyle: new(12, "FontName"), TabInfoId: 1, SpecialType: SpecialDetailType.Current),
                    new("PropNameTempTarget", 3, EditInfo: new(MessageType.Update, Command.Temp), TextStyle: new(12, "FontName"), TabInfoId: 1, SpecialType: SpecialDetailType.Target)
                },
                TabInfos: new()
                {
                    new(0, "DefaultIcon", 1),
                    new(1, "IconName", 2),
                    new(2, "IconName", 3, new("LinkedDeviceIdPropertyName", "DeviceTypeName")),
                },
                new() { }));
            var serializedDemo = JsonConvert.SerializeObject(deviceLayout, Formatting.Indented);
            File.WriteAllText(DemoFilePath, serializedDemo);
        }
        ReloadLayouts();
    }

    private static void FileChanged(object sender, FileSystemEventArgs e)
    {
        _ = Task.Delay(100).ContinueWith((t) => { 
            var layout = JsonConvert.DeserializeObject<DeviceLayout>(File.ReadAllText(e.FullPath));

            if (layout is null)
                return;
            if (layout.TypeName is not null)
                TypeDeviceLayouts[layout.TypeName] = layout;

            if (layout.Ids is not null)
            {
                foreach (var id in layout.Ids)
                {
                    InstanceDeviceLayouts[id] = layout;
                }
            }

            foreach (var device in IInstanceContainer.Instance.DeviceManager.Devices)
            {
                if (layout.Ids?.Contains(device.Key) ?? false)
                {
                    foreach (var sub in device.Value.Subscribers)
                    {
                        _ = sub.SmarthomeClient.UpdateUi(layout);
                    }
                    continue;
                }
                if (layout.TypeName is not null && device.Value.TypeNames.Contains(layout.TypeName))
                {
                    foreach (var sub in device.Value.Subscribers)
                    {
                        _ = sub.SmarthomeClient.UpdateUi(layout);
                    }
                    continue;
                }
            }
        });
    }

    public static void ReloadLayouts()
    {
        var files = Directory.GetFiles("DeviceLayouts", "*.json");

        TypeDeviceLayouts.Clear();
        InstanceDeviceLayouts.Clear();
        foreach (var file in files)
        {
            // TODO: try catch
            try
            {
                var layout = Newtonsoft.Json.JsonConvert.DeserializeObject<DeviceLayout>(File.ReadAllText(file));

                if (layout is null)
                    continue;

                if (layout.TypeName is not null && !TypeDeviceLayouts.ContainsKey(layout.TypeName))
                    TypeDeviceLayouts.Add(layout.TypeName, layout);
                else
                {/* TODO: warning*/}

                if (layout.Ids is not null)
                {
                    foreach (var id in layout.Ids)
                    {
                        if (InstanceDeviceLayouts.ContainsKey(id))
                            continue;
                        InstanceDeviceLayouts.Add(id, layout);
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    public static DetailDeviceLayout? GetDetailDeviceLayout(string typeName)
        => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret.DetailDeviceLayout : null;

    public static DetailDeviceLayout? GetDetailDeviceLayout(long deviceId)
    {
        if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
        {
            return ret.DetailDeviceLayout;
        }
        else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device))
        {
            if (InstanceDeviceLayouts.TryGetValue(device.Id, out ret))
            {
                return ret.DetailDeviceLayout;
            }
        }
        return null;

    }

    public static DashboardDeviceLayout? GetDashboardDeviceLayout(string typeName)
        => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret.DashboardDeviceLayout : null;

    public static DashboardDeviceLayout? GetDashboardDeviceLayout(long deviceId)
    {
        if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
        {
            return ret.DashboardDeviceLayout;
        }
        else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device))
        {
            if (InstanceDeviceLayouts.TryGetValue(device.Id, out ret))
            {
                return ret.DashboardDeviceLayout;
            }
        }
        return null;

    }


    public static DeviceLayout? GetDeviceLayout(string typeName)
        => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret : null;

    public static DeviceLayout? GetDeviceLayout(long deviceId)
    {
        if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
        {
            return ret;
        }
        else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device))
        {
            if (InstanceDeviceLayouts.TryGetValue(device.Id, out ret))
            {
                return ret;
            }
        }
        return null;

    }

}
