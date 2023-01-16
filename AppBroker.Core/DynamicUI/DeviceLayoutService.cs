using Newtonsoft.Json;

using System.Security.Cryptography;

namespace AppBroker.Core.DynamicUI;

public static class DeviceLayoutService
{
    public static Dictionary<string, (DeviceLayout layout, string hash)> TypeDeviceLayouts = new();
    public static Dictionary<long, (DeviceLayout layout, string hash)> InstanceDeviceLayouts = new();

    private static readonly FileSystemWatcher fileSystemWatcher;

    static DeviceLayoutService()
    {
        var deviceLayoutsPath = new DirectoryInfo("DeviceLayouts").FullName;
        Directory.CreateDirectory(deviceLayoutsPath);
        fileSystemWatcher = new FileSystemWatcher(deviceLayoutsPath, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.LastWrite |
                NotifyFilters.Security
        };
        fileSystemWatcher.Changed += FileChanged;
        fileSystemWatcher.Created += FileChanged;
        fileSystemWatcher.EnableRaisingEvents = true;

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
        var logger = NLog.LogManager.GetCurrentClassLogger();

        _ = Task.Delay(100).ContinueWith((t) =>
        {
            logger.Info($"Layout change detected: {e.FullPath}");
            var (layout, hash) = GetHashAndFile(e.FullPath);

            if (layout is null)
                return;

            HashSet<string> subs = new();

            CheckLayout(layout, hash, subs);
            if (layout.TypeNames is not null)
            {
                foreach (var item in layout.TypeNames)
                {
                    CheckLayout(layout with { TypeName = item }, hash, subs);

                }
            }

            CheckLayoutIds(layout, hash, subs);
        });
    }
    private static void CheckLayoutIds(DeviceLayout layout, string hash, HashSet<string> subs)
    {
        bool updated = false;
        if (layout.Ids is not null)
        {
            foreach (long id in layout.Ids)
            {
                if (InstanceDeviceLayouts.TryGetValue(id, out var layoutType) && layoutType.hash == hash)
                    continue;

                InstanceDeviceLayouts[id] = (layout, hash);
                updated = true;
            }
        }

        if (!updated)
            return;

        foreach (KeyValuePair<long, Devices.Device> device in IInstanceContainer.Instance.DeviceManager.Devices)
        {
            if (layout.Ids?.Contains(device.Key) ?? false)
            {
                foreach (var sub in device.Value.Subscribers)
                {
                    if (subs.Add(sub.ConnectionId))
                        _ = sub.SmarthomeClient.UpdateUi(layout, hash);
                }
            }
        }
    }
    private static void CheckLayout(DeviceLayout layout, string hash, HashSet<string> subs)
    {
        bool updated = false;
        if (layout.TypeName is not null)
        {
            if (!TypeDeviceLayouts.TryGetValue(layout.TypeName, out var layoutType) || layoutType.hash != hash)
            {
                TypeDeviceLayouts[layout.TypeName] = (layout, hash);
                updated = true;
            }
        }

        if (!updated)
            return;
        foreach (KeyValuePair<long, Devices.Device> device in IInstanceContainer.Instance.DeviceManager.Devices)
        {

            if (layout.TypeName is not null && device.Value.TypeNames.Contains(layout.TypeName))
            {
                foreach (var sub in device.Value.Subscribers)
                {
                    if (subs.Add(sub.ConnectionId))
                        _ = sub.SmarthomeClient.UpdateUi(layout, hash);
                }
                continue;
            }
        }
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
                else if (layout.TypeNames is not null)
                {
                    foreach (var item in layout.TypeNames)
                    {
                        if (!TypeDeviceLayouts.ContainsKey(item))
                            TypeDeviceLayouts.Add(item, (layout, hash));
                    }
                }

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



    public static (DeviceLayout? layout, string hash)? GetDeviceLayout(string typeName)
        => TypeDeviceLayouts.TryGetValue(typeName, out var ret) ? ret : null;

    public static (DeviceLayout? layout, string hash, bool byId)? GetDeviceLayout(long deviceId)
    {
        if (InstanceDeviceLayouts.TryGetValue(deviceId, out var ret))
        {
            return (ret.layout, ret.hash, true);
        }
        else if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Devices.Device? device))
        {
            if (TypeDeviceLayouts.TryGetValue(device.TypeName, out ret))
            {
                return (ret.layout, ret.hash, false);
            }
            foreach (string typeName in device.TypeNames)
            {
                if (TypeDeviceLayouts.TryGetValue(typeName, out ret))
                {
                    return (ret.layout, ret.hash, false);
                }
            }

        }
        return null;

    }

    public static List<DeviceLayout> GetAllLayouts()
    {
        return InstanceDeviceLayouts
            .Values
            .Select(x => x.layout)
            .Concat(TypeDeviceLayouts
                .Values
                .Select(x => x.layout))
            .Distinct()
            .ToList();
    }

}
