using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;

public class LayoutBasePropertyInfo
{
    public string Name { get; set; } = "";
    public int Order { get; set; }
    public TextSettings? TextStyle { get; set; }
    public PropertyEditInformation? EditInfo { get; set; }
    public int? RowNr { get; set; }
    public string UnitOfMeasurement { get; set; } = "";
    public string Format { get; set; } = "";
    public bool? ShowOnlyInDeveloperMode { get; set; }
    public long? DeviceId { get; set; }
    public bool? Expanded { get; set; }
    public int? Precision { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionDataDes { get; set; }
    public Dictionary<string, JToken>? ExtensionData => ExtensionDataDes;

    public string DisplayName { get; set; } = "";
}