using AppBroker.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.Core;

public abstract class BaseSmarthomeMessage
{
    [JsonProperty("id")]
    public virtual uint NodeId { get; set; }

    [JsonProperty("m"), JsonConverter(typeof(StringEnumConverter))]
    public virtual MessageType MessageType { get; set; }

    [JsonProperty("c"), JsonConverter(typeof(StringEnumConverter))]
    public virtual Command Command { get; set; }
}
