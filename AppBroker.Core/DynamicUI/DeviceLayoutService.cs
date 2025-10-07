using Json.Path;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

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
    private static string GetMD5StringFor(ReadOnlySpan<byte> bytes)
    {
        Span<byte> toWriteBytes = stackalloc byte[16];
        _ = MD5.HashData(bytes, toWriteBytes);
        return Convert.ToHexString(toWriteBytes);
    }
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms

    private static DeviceLayout GetLayout(string fileName, JToken rawData)
    {
        var hash = GetMD5StringFor(Encoding.UTF8.GetBytes(rawData.ToString()));

        var layout = rawData.ToObject<DeviceLayout>();
        if (layout is null)
        {
            throw new FileLoadException($"Could not parse {fileName} to DeviceLayout");
        }
        return layout with { AdditionalDataDes = layout.AdditionalDataDes ?? [], Hash = hash };

    }
    private static void FileChanged(object sender, FileSystemEventArgs e)
    {
        var logger = NLog.LogManager.GetCurrentClassLogger();

        _ = Task.Delay(100).ContinueWith((t) =>
        {
            logger.Info($"Layout change detected: {e.FullPath}");

            var (name, token) = GetLayoutsWithPathReplaced("DeviceLayouts").FirstOrDefault(x => x.Key == e.Name);

            var layout = GetLayout(name, token);

            if (layout is null)
                return;

            HashSet<string> subs = new();

            CheckLayout(layout, layout.Hash, subs);
            if (layout.TypeNames is not null)
            {
                foreach (var item in layout.TypeNames)
                {
                    CheckLayout(layout with { TypeName = item }, layout.Hash, subs);

                }
            }

            CheckLayoutIds(layout, layout.Hash, subs);
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
        var layouts = GetLayoutsWithPathReplaced("DeviceLayouts");

        TypeDeviceLayouts.Clear();
        InstanceDeviceLayouts.Clear();

        foreach (var (name, token) in layouts.Where(x => x.Value["UniqueName"] is not null))
        {
            try
            {
                var layout = GetLayout(name, token);
                var hash = layout.Hash;
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

    const string refStr = "\"@ref\"";
    private static Dictionary<string, JToken> GetLayoutsWithPathReplaced(string directory)
    {
        StringBuilder sb = new();
        sb.Append("{");
        foreach (var path in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories))
        {
            sb.Append(
                $$"""
                "{{Path.GetRelativePath(directory, path).Replace('\\', '/')}}" : {{File.ReadAllText(path)}},
                """);
        }

        sb.Remove(sb.Length - 1, 1);
        sb.Append("}");
        var allCombined = sb.ToString();
        var jo = JsonNode.Parse(allCombined,
            documentOptions:
            new System.Text.Json.JsonDocumentOptions()
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });
        int currentIndex = 0;
        while (currentIndex < allCombined.Length)
        {
            var refIndex = allCombined.IndexOf(refStr, currentIndex);
            if (refIndex == -1)
                break;
            int refFrom = 0;
            int refTo = 0;
            byte foundQuotations = 0;
            for (int i = refIndex + refStr.Length; i < allCombined.Length - 1; i++)
            {
                if (allCombined[i] == '"' && allCombined[i - 1] != '\\')
                {
                    foundQuotations++;
                    if (foundQuotations == 1)
                    {
                        refFrom = i;
                    }
                    else
                    {
                        refTo = i + 1;
                        break;
                    }
                }
            }
            if (foundQuotations < 2)
                break;
            refIndex = allCombined.LastIndexOf('{', refIndex);
            var removeTo = allCombined.IndexOf('}', refTo) + 1;
            var path = allCombined[(refFrom + 1)..(refTo - 1)];
            allCombined = allCombined.Remove(refIndex, removeTo - refIndex);

            var jPath = JsonPath.Parse(path);
            var res = jPath.Evaluate(jo);
            if (res.Matches.Count == 0)
            {
                throw new JsonPathNotResolvableException(path);
            }
            var match = res.Matches[0];
            var key = jPath.Segments.Last().Selectors.Last().ToString().Replace("'", "\"");
            allCombined = allCombined.Insert(refIndex, match.Value.ToJsonString());
            currentIndex = refIndex;
        }

        return
            JsonConvert
            .DeserializeObject<Dictionary<string, JToken>>(allCombined);

    }
    private class JsonPathNotResolvableException : Exception
    {
        public JsonPathNotResolvableException(string? path) : base($"The json path ´{path}´ is not resolvable.")
        {
            
        }
    }
}
