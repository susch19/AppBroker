namespace AppBroker.Core.Devices;

[AttributeUsage(AttributeTargets.Class)]
public class DeviceNameAttribute : Attribute
{
    public DeviceNameAttribute(string alternateName, params string[] alternativeNames)
    {
        PreferredName = alternateName;
        AlternativeNames = alternativeNames.ToList();
    }

    public string PreferredName { get; }

    public List<string> AlternativeNames { get; set; }
}
