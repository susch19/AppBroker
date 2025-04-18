using AppBroker.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.Core;

public abstract class BaseSmarthomeMessage
{
    [JsonProperty("id")]
    public virtual long NodeId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public virtual MessageType MessageType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    public virtual Command Command { get; set; }
}
