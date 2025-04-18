using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;

public class EditParameter
{
    public Command Command { get; set; }
    public JToken Value { get; set; } = default!;
    public int? Id { get; set; }
    public MessageType? MessageType { get; set; }
    public string? DisplayName { get; set; }
    public List<JToken>? Parameters { get; set; }
    [JsonExtensionData]
    public Dictionary<string, JToken>? ExtensionDataDes { get; set; }
    public Dictionary<string, JToken>? ExtensionData => ExtensionDataDes;
}
