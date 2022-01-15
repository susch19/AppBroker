
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

[JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(CamelCaseNamingStrategy))]
public enum SpecialType
{
    None = 0,
    Battery = 1,
    Disabled = 2,
    Color = 3,
    //Availability = 3,
}

public record DashbardPropertyInfo(string Name, int Order, string Format = "", PropertyEditInformation? EditInfo = null, TextStyle? TextStyle = null, int? RowNr = null, string UnitOfMeasurement = "", SpecialType SpecialType = SpecialType.None, bool? ShowOnlyInDeveloperMode = null);
