using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

/// <summary>
/// The thickness of the glyphs used to draw the text
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum FontWeightSetting
{
    Normal,
    Bold
}
