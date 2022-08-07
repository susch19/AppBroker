using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;

public class PropertyEditInformation
{
    public MessageType EditCommand { get; set; }
    public List<EditParameter> EditParameter { get; set; } = default!;
    public EditType EditType { get; set; }
    public string? Display { get; set; }
    public string? HubMethod { get; set; }
    public string? ValueName { get; set; }
    public object? ActiveValue { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionData { get; set; }
}


