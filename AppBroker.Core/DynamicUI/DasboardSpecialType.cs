
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

[JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(CamelCaseNamingStrategy))]
public enum DasboardSpecialType
{
    None = 0,
    Right = 1,
    //Disabled = 2,
    //Color = 3,
    //Availability = 3,
}
