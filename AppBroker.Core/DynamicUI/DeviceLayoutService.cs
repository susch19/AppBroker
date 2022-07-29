using AppBrokerASP;

using Newtonsoft.Json;

using System.Security.Cryptography;

namespace AppBroker.Core.DynamicUI;

public static class DeviceLayoutService
{
    public static Dictionary<string, (DeviceLayout layout, string hash)> TypeDeviceLayouts = new();
    public static Dictionary<long, (DeviceLayout layout, string hash)> InstanceDeviceLayouts = new();

    private static readonly string demoFilePath = Path.Combine("DeviceLayouts", "SampleLayout.json");
    private static readonly FileSystemWatcher fileSystemWatcher;

    static DeviceLayoutService()
    {
        fileSystemWatcher = new FileSystemWatcher(new DirectoryInfo("DeviceLayouts").FullName, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        fileSystemWatcher.Changed += FileChanged;
        fileSystemWatcher.Created += FileChanged;
        fileSystemWatcher.EnableRaisingEvents = true;

        if (!File.Exists(demoFilePath))
        {
            var deviceLayout = new DeviceLayout("DemoLayout", "Demo",
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
            string? serializedDemo = JsonConvert.SerializeObject(deviceLayout, Formatting.Indented);
            File.WriteAllText(demoFilePath, serializedDemo);
        }
        ReloadLayouts();
    }

#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
    private static string GetMD5StringFor(byte[] bytes)
    {
        Span<byte> toWriteBytes = stackalloc byte[16];
        _ = MD5.HashData(bytes, toWriteBytes);
        return Convert.ToHexString(toWriteBytes);
    }
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

    private static (DeviceLayout? layout, string hash) GetHashAndFile(string path)
    {
        var text = File.ReadAllText(path);
        var hash = GetMD5StringFor(File.ReadAllBytes(path));
        return (JsonConvert.DeserializeObject<DeviceLayout>(text), hash);
    }
    private static void FileChanged(object sender, FileSystemEventArgs e)
    {
        _ = Task.Delay(100).ContinueWith((t) =>
        {
            var (layout, hash) = GetHashAndFile(e.FullPath);
            bool updated = false;

            if (layout is null)
                return;
            if (layout.TypeName is not null)
            {
                if (!TypeDeviceLayouts.TryGetValue(layout.TypeName, out var layoutType) || layoutType.hash != hash)
                {
                    TypeDeviceLayouts[layout.TypeName] = (layout, hash);
                    updated = true;
                }
            }

            if (layout.Ids is not null)
            {
                foreach (long id in layout.Ids)
                {
                    if (InstanceDeviceLayouts.TryGetValue(id, out var layoutType) && layoutType.hash == hash)
                        continue;
                    {
                        InstanceDeviceLayouts[id] = (layout, hash);

                    }
                }
            }

            if (!updated)
                return;

            foreach (KeyValuePair<long, Devices.Device> device in IInstanceContainer.Instance.DeviceManager.Devices)
            {
                if (layout.Ids?.Contains(device.Key) ?? false)
                {
                    foreach (Subscriber? sub in device.Value.Subscribers)
                    {
                        _ = sub.SmarthomeClient.UpdateUi(layout, hash);
                    }
                    continue;
                }
                if (layout.TypeName is not null && device.Value.TypeNames.Contains(layout.TypeName))
                {
                    foreach (Subscriber? sub in device.Value.Subscribers)
                    {
                        _ = sub.SmarthomeClient.UpdateUi(layout, hash);
                    }
                    continue;
                }
            }
        });
    }

    public static void ReloadLayouts()
    {
        string[]? files = Directory.GetFiles("DeviceLayouts", "*.json");

        TypeDeviceLayouts.Clear();
        InstanceDeviceLayouts.Clear();
        foreach (string? file in files)
        {
            // TODO: try catch
            try
            {
                var (layout, hash) = GetHashAndFile(file);

                if (layout is null)
                    continue;

                if (layout.TypeName is not null && !TypeDeviceLayouts.ContainsKey(layout.TypeName))
                    TypeDeviceLayouts.Add(layout.TypeName, (layout, hash));
                else
                {/* TODO: warning*/}

                if (layout.Ids is not null)
                {
                    foreach (long id in layout.Ids)
                    {
                        if (InstanceDeviceLayouts.ContainsKey(id))
                            continue;
                        InstanceDeviceLayouts.Add(id, (layout, hash));
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    //public static DetailDeviceLayout? GetDetailDeviceLayout(string typeName)
    //    => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret.layout.DetailDeviceLayout : null;

    //public static DetailDeviceLayout? GetDetailDeviceLayout(long deviceId)
    //{
    //    if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
    //    {
    //        return ret.layout.DetailDeviceLayout;
    //    }
    //    else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Devices.Device? device))
    //    {
    //        foreach (string typeName in device.TypeNames)
    //        {
    //            DetailDeviceLayout? res = GetDetailDeviceLayout(typeName);
    //            if (res is not null)
    //                return res;
    //        }
    //    }
    //    return null;

    //}

    //public static DashboardDeviceLayout? GetDashboardDeviceLayout(string typeName)
    //    => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret.layout.DashboardDeviceLayout : null;

    //public static DashboardDeviceLayout? GetDashboardDeviceLayout(long deviceId)
    //{
    //    if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
    //    {
    //        return ret.layout.DashboardDeviceLayout;
    //    }
    //    else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Devices.Device? device))
    //    {
    //        foreach (string typeName in device.TypeNames)
    //        {
    //            DashboardDeviceLayout? res = GetDashboardDeviceLayout(typeName);
    //            if (res is not null)
    //                return res;
    //        }
    //    }
    //    return null;

    //}


    public static (DeviceLayout? layout, string hash)? GetDeviceLayout(string typeName)
        => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret : null;

    public static (DeviceLayout? layout, string hash)? GetDeviceLayout(long deviceId)
    {
        if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
        {
            return ret;
        }
        else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Devices.Device? device))
        {
            foreach (string typeName in device.TypeNames)
            {
                if (TypeDeviceLayouts.TryGetValue(typeName, out ret))
                {
                    return ret;
                }
            }
        }
        return null;

    }

}
