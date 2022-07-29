using AppBrokerASP;

using Newtonsoft.Json;

using System.Security.Cryptography;

namespace AppBroker.Core;

[NonSucking.Framework.Serialization.Nooson]

public partial record SvgIcon(string Name, string Hash, [property: JsonIgnore] string Path, byte[]? Data);


#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
public class IconService
{
    private readonly Dictionary<string, SvgIcon> iconCache = new();
    private readonly SvgIcon fallBackIcon;

    public IconService()
    {
        var fallbackPath = Path.Combine("Icons", "paw.svg");
        var fallbackData = File.ReadAllBytes(fallbackPath);

        fallBackIcon = new("Paw", GetMD5StringFor(fallbackData), fallbackPath, fallbackData);
    }

    private string GetMD5StringFor(byte[] bytes)
    {
        Span<byte> toWriteBytes = stackalloc byte[16];
        _ = MD5.HashData(bytes, toWriteBytes);
        return Convert.ToHexString(toWriteBytes);
    }

    public SvgIcon GetIconByName(string iconName)
    {
        if (iconCache.TryGetValue(iconName, out var result))
            return result;

        var path = Path.Combine("Icons", iconName + ".svg");

        if (!File.Exists(path))
            return fallBackIcon;

        var iconBytes = File.ReadAllBytes(path);
        return iconCache[iconName] = new(iconName, GetMD5StringFor(iconBytes), path, iconBytes);
    }

    public SvgIcon GetBestFitIcon(string typeName)
    {
        if (iconCache.TryGetValue(typeName, out var result))
            return result;

        result = fallBackIcon;

        var device = IInstanceContainer
            .Instance
            .DeviceManager
            .Devices
            .Values
            .FirstOrDefault(x => x.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        if (device == default)
            return result;

        foreach (var tmpTypeName in device.TypeNames)
        {
            var path = Path.Combine("Icons", tmpTypeName + ".svg");

            if (!File.Exists(path))
                continue;

            var iconBytes = File.ReadAllBytes(path);
            var existing = iconCache.FirstOrDefault(x => x.Value.Path == path).Value;

            result = existing == default ? new(tmpTypeName, GetMD5StringFor(iconBytes), path, iconBytes) : existing;
            break;
        }

        if (result != fallBackIcon)
            iconCache[typeName] = result;

        return result;
    }

}
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
