using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;

public class LayoutBasePropertyInfo
{
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public TextStyle? TextStyle { get; set; }
    public PropertyEditInformation? EditInfo { get; set; }
    public int? RowNr { get; set; }
    public string UnitOfMeasurement { get; set; } = "";
    public string Format { get; set; } = "";
    public bool? ShowOnlyInDeveloperMode { get; set; }
    public long? DeviceId { get; set; }
    public bool? Expanded { get; set; }
    public int? Precision { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionData { get; set; }

    public string DisplayName { get; set; } = "";
}

