using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;

public class PropertyEditInformation
{
    public MessageType MessageType { get; set; }
    public List<EditParameter> EditParameter { get; set; } = default!;
    public string EditType { get; set; }
    public string? Display { get; set; }
    public string? HubMethod { get; set; }
    public string? ValueName { get; set; }
    public object? ActiveValue { get; set; }
    public string? Dialog { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionDataDes { get; set; }
    public Dictionary<string, JToken>? ExtensionData => ExtensionDataDes;
}


