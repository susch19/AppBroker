using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.Core;
[JsonConverter(typeof(StringEnumConverter))]
public enum MessageType
{
    Get,
    Update,
    Options,
    Relay
};
