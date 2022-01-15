using AppBrokerASP;

namespace AppBroker.Core;

public class IconService
{
    Dictionary<string, byte[]> iconCache = new();
    readonly byte[] fallBackIcon = File.ReadAllBytes(Path.Combine("Icons", "paw.svg"));

    public byte[] GetIcon(string typeName)
    {
        return GetIconByName(typeName);
    }

    public byte[] GetIconByName(string iconName)
    {
        if (iconCache.TryGetValue(iconName, out var result))
            return result;

        var path = Path.Combine("Icons", iconName + ".svg");

        if (!File.Exists(path))
            return fallBackIcon;

        result = File.ReadAllBytes(path);
        iconCache[iconName] = result;

        return result;
    }

    public byte[] GetBestFitIcon(string typeName)
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

            result = File.ReadAllBytes(path);
            break;
        }

        if (result != fallBackIcon)
            iconCache[typeName] = result;

        return result;
    }

}
