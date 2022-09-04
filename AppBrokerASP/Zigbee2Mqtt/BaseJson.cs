using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Runtime.Serialization;

namespace AppBrokerASP.Zigbee2Mqtt;

#nullable disable
internal abstract class BaseJson
{
    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; }
}

internal class Clusters : BaseJson
{
    [JsonProperty("input")]
    public string[] Input { get; set; }

    [JsonProperty("output")]
    public string[] Output { get; set; }
}

internal class DeviceDefinition : BaseJson
{
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("model")]
    public string Model { get; set; }

    [JsonProperty("supports")]
    public string Supports { get; set; }

    [JsonProperty("vendor")]
    public string Vendor { get; set; }

    [JsonProperty("exposes")]
    public GenericExposedFeature[] Exposes { get; set; }

    [JsonProperty("options")]
    public GenericExposedFeature[] Options { get; set; }

    [JsonProperty("supports_ota")]
    public bool SupportsOta { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }
}

internal class ReportingConfig : BaseJson
{
    [JsonProperty("cluster")]
    public string Cluster { get; set; }

    [JsonProperty("attribute")]
    public string Attribute { get; set; }

    [JsonProperty("maximum_report_interval")]
    public float MaximumReportInterval { get; set; }

    [JsonProperty("minimum_report_interval")]
    public float MinimumReportInterval { get; set; }

    [JsonProperty("reportable_change")]
    public JToken ReportableChange { get; set; }
}

internal class EndpointDescription : BaseJson
{
    [JsonProperty("bindings")]
    public BindRule[] Bindings { get; set; }

    [JsonProperty("configured_reportings")]
    public ReportingConfig[] ConfiguredReportings { get; set; }

    [JsonProperty("clusters")]
    public Clusters Clusters { get; set; }

    [JsonProperty("scenes")]
    public Scene[] Scenes { get; set; }
}

internal class Scene : BaseJson
{
    [JsonProperty("id")]
    public float Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }
}

internal class Group : BaseJson
{
    [JsonProperty("id")]
    public float Id { get; set; }

    [JsonProperty("members")]
    public GroupAddress[] Members { get; set; }

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("scenes")]
    public Scene[] Scenes { get; set; }
}

[DebuggerDisplay("{FriendlyName} {ModelId}")]
internal class Device : BaseJson
{
    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }

    [JsonProperty("type")]
    public DeviceType Type { get; set; }

    [JsonProperty("network_address")]
    public float NetworkAddress { get; set; }

    [JsonProperty("power_source")]
    public PowerSource PowerSource { get; set; }

    [JsonProperty("model_id")]
    public string ModelId { get; set; }

    [JsonProperty("manufacturer")]
    public string Manufacturer { get; set; }

    [JsonProperty("interviewing")]
    public bool Interviewing { get; set; }

    [JsonProperty("interview_completed")]
    public bool InterviewCompleted { get; set; }

    [JsonProperty("software_build_id")]
    public string SoftwareBuildId { get; set; }

    [JsonProperty("supported")]
    public bool Supported { get; set; }

    [JsonProperty("definition")]
    public DeviceDefinition Definition { get; set; }

    [JsonProperty("date_code")]
    public string DateCode { get; set; }

    [JsonProperty("endpoints")]
    public Dictionary<string, EndpointDescription> Endpoints { get; set; }

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

internal class BindRule : BaseJson
{
    [JsonProperty("cluster")]
    public string Cluster { get; set; }

    [JsonProperty("target")]
    public Target Target { get; set; }
}

internal class Target : BaseJson
{
    [JsonProperty("id")]
    public float Id { get; set; }

    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }

    [JsonProperty("ieee_address")]
    public string IEEEEAddress { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }
}

internal class GroupAddress : BaseJson
{
    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }

    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }
}

internal class Meta : BaseJson
{
    [JsonProperty("revision")]
    public float Revision { get; set; }

    [JsonProperty("transportrev")]
    public float Transportrev { get; set; }

    [JsonProperty("product")]
    public float Product { get; set; }

    [JsonProperty("majorrel")]
    public float Majorrel { get; set; }

    [JsonProperty("minorrel")]
    public float Minorrel { get; set; }

    [JsonProperty("maintrel")]
    public float Maintrel { get; set; }
}

internal class Coordinator : BaseJson
{
    [JsonProperty("channel")]
    public float Channel { get; set; }

    [JsonProperty("pan_id")]
    public float PanId { get; set; }

    [JsonProperty("extended_pan_id")]
    public float[] ExtendedPanId { get; set; }
}

internal class Z2MConfig : BaseJson
{
    [JsonProperty("homeassistant")]
    public bool Homeassistant { get; set; }

    [JsonProperty("advanced")]
    public AdvancedConfig Advanced { get; set; }

    [JsonProperty("devices")]
    public Dictionary<string, Dictionary<string, object>> Devices { get; set; }

    [JsonProperty("device_options")]
    public Dictionary<string, object> DeviceOptions { get; set; }
}

internal class BridgeConfig : BaseJson
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("commit")]
    public string Commit { get; set; }

    [JsonProperty("coordinator")]
    public Coordinator Coordinator { get; set; }

    [JsonProperty("network")]
    public Network Network { get; set; }

    [JsonProperty("log_level")]
    public string LogLevel { get; set; }

    [JsonProperty("permit_join")]
    public bool PermitJoin { get; set; }
}

internal class LogMessage
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("level")]
    public LogLevel Level { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum LogLevel
{
    [EnumMember(Value = "error")]
    Error,

    [EnumMember(Value = "info")]
    Info,

    [EnumMember(Value = "warning")]
    Warning
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum BridgeState
{
    [EnumMember(Value = "online")]
    Online,

    [EnumMember(Value = "offline")]
    Offline,
}

internal class BridgeInfo : BaseJson
{
    [JsonProperty("config")]
    public Z2MConfig Config { get; set; }

    [JsonProperty("config_schema")]
    public object ConfigSchema { get; set; }

    [JsonProperty("permit_join")]
    public bool PermitJoin { get; set; }

    [JsonProperty("permit_join_timeout")]
    public float PermitJoinTimeout { get; set; }

    [JsonProperty("commit")]
    public string Commit { get; set; }

    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("coordinator")]
    public Coordinator1 Coordinator { get; set; }

    [JsonProperty("device_options")]
    public Dictionary<string, object> DeviceOptions { get; set; }

    [JsonProperty("restart_required")]
    public bool RestartRequired { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum GenericFeatureType
{
    [EnumMember(Value = "numeric")]
    Numeric,

    [EnumMember(Value = "binary")]
    Binary,

    [EnumMember(Value = "enum")]
    Enum,

    [EnumMember(Value = "text")]
    Text,

    [EnumMember(Value = "list")]
    List,

    [EnumMember(Value = "fan")]
    Fan,

    [EnumMember(Value = "light")]
    Light,

    [EnumMember(Value = "switch")]
    Switch,

    [EnumMember(Value = "cover")]
    Cover,

    [EnumMember(Value = "lock")]
    Lock,

    [EnumMember(Value = "composite")]
    Composite,

    [EnumMember(Value = "climate")]
    Climate
}

[Flags]
[JsonConverter(typeof(StringEnumConverter))]
internal enum FeatureAccessMode
{
    [EnumMember(Value = "NONE")]
    None = 0,

    [EnumMember(Value = "ACCESS_STATE")]
    State = 1,

    [EnumMember(Value = "ACCESS_WRITE")]
    Write = 2,

    [EnumMember(Value = "ACCESS_READ")]
    Read = 4,
}

internal class FeatureJsonConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => typeof(GenericExposedFeature).IsAssignableFrom(objectType);

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var jObject = JObject.Load(reader);

        var type = jObject.Value<string>("type");
        var targetType = type switch
        {
            "numeric" => new NumericFeature(),
            "binary" => new BinaryFeature(),
            "enum" => new EnumFeature(),
            "list" => new ListFeature(),
            "fan" => new FanFeature(),
            "light" => new LightFeature(),
            "switch" => new SwitchFeature(),
            "cover" => new CoverFeature(),
            "lock" => new LockFeature(),
            "climate" => new ClimateFeature(),

            _ => Default(),
        };

        serializer.Populate(jObject.CreateReader(), targetType);

        return targetType;

        GenericExposedFeature Default()
        {
            var name = jObject.Value<string>("name");
            return name switch
            {
                "color_hs" or "color_xy" => new ColorFeature(),
                _ => new GenericExposedFeature()
            };
        }
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }
}

[DebuggerDisplay("{Type} {Name}")]
[JsonConverter(typeof(FeatureJsonConverter))]
internal class GenericExposedFeature : BaseJson
{
    [JsonProperty("type")]
    public GenericFeatureType Type { get; set; }


    [JsonProperty("name")]
    public string Name { get; set; }


    [JsonProperty("unit")]
    public string Unit { get; set; } = "string";


    [JsonProperty("access")]
    public FeatureAccessMode Access { get; set; }


    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }


    [JsonProperty("property")]
    public string Property { get; set; }


    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("features")]
    public GenericExposedFeature[] Features { get; set; }
}

internal class BinaryFeature : GenericExposedFeature
{
    [JsonProperty("value_on")]
    public object ValueOn { get; set; }

    [JsonProperty("value_off")]
    public object ValueOff { get; set; }

    [JsonProperty("value_toggle")]
    public object ValueToggle { get; set; }

    public BinaryFeature()
    {
        Type = GenericFeatureType.Binary;
    }
}

internal class ListFeature : GenericExposedFeature
{
    [JsonProperty("item_type")]
    public string ItemType { get; set; } = "number";

    public ListFeature()
    {
        Type = GenericFeatureType.List;
    }
}

internal class NumericFeaturePreset : BaseJson
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public float Value { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

internal class NumericFeature : GenericExposedFeature
{
    [JsonProperty("value_min")]
    public float ValueMin { get; set; }

    [JsonProperty("value_max")]
    public float ValueMax { get; set; }

    [JsonProperty("value_step")]
    public float ValueStep { get; set; }

    [JsonProperty("presets")]
    public NumericFeaturePreset[] Presets { get; set; }

    public NumericFeature()
    {
        Type = GenericFeatureType.Numeric;
    }
}

internal class TextualFeature : GenericExposedFeature
{
    public TextualFeature()
    {
        Type = GenericFeatureType.Text;
    }
}

internal class EnumFeature : GenericExposedFeature
{
    [JsonProperty("values")]
    public object[] Values { get; set; }

    public EnumFeature()
    {
        Type = GenericFeatureType.Enum;
    }
}

internal class LightFeature : GenericExposedFeature
{
    public LightFeature()
    {
        Type = GenericFeatureType.Light;
    }
}

internal class SwitchFeature : GenericExposedFeature
{
    public SwitchFeature()
    {
        Type = GenericFeatureType.Switch;
    }
}

internal class CoverFeature : GenericExposedFeature
{
    public CoverFeature()
    {
        Type = GenericFeatureType.Cover;
    }
}

internal class LockFeature : GenericExposedFeature
{
    public LockFeature()
    {
        Type = GenericFeatureType.Lock;
    }
}
internal class FanFeature : GenericExposedFeature
{
    public FanFeature()
    {
        Type = GenericFeatureType.Fan;
    }
}

internal class ClimateFeature : GenericExposedFeature
{
    public ClimateFeature()
    {
        Type = GenericFeatureType.Climate;
    }
}

internal class ColorFeature : GenericExposedFeature
{
    [JsonProperty("name")]
    public new ColorName Name { get; set; }

    [JsonProperty("features")]
    public new NumericFeature[] Features { get; set; }

    public ColorFeature()
    {
        Type = GenericFeatureType.Composite;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum ColorName
{
    [EnumMember(Value = "color_xy")]
    ColorXY,

    [EnumMember(Value = "color_hs")]
    ColorHS,
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum PowerSource
{
    [EnumMember(Value = "Battery")]
    Battery,

    [EnumMember(Value = "Mains (single phase)")]
    Mains,

    [EnumMember(Value = "DC Source")]
    DC,
}

internal class Meta1 : BaseJson
{
    [JsonProperty("revision")]
    public string Revision { get; set; }
}

internal class Coordinator1 : BaseJson
{
    [JsonProperty("meta")]
    public Meta1 Meta { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }
}


internal class TouchLinkDevice : BaseJson
{
    [JsonProperty("ieee_address")]
    public string IEEEEAddress { get; set; }

    [JsonProperty("channel")]
    public float Channel { get; set; }
}


[JsonConverter(typeof(StringEnumConverter))]
internal enum LastSeen
{
    [EnumMember(Value = "disable")]
    Disable,

    [EnumMember(Value = "ISO_8601")]
    ISO_8601,

    [EnumMember(Value = "ISO_8601_local")]
    ISO_8601_local,

    [EnumMember(Value = "epoch")]
    Epoch
}

internal class AdvancedConfig : BaseJson
{
    [JsonProperty("elapsed")]
    public bool Elapsed { get; set; }

    [JsonProperty("last_seen")]
    public LastSeen LastSeen { get; set; }

    [JsonProperty("legacy_api")]
    public bool LegacyApi { get; set; }
}

internal class Network : BaseJson
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("meta")]
    public Meta Meta { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum DeviceType
{
    [EnumMember(Value = "EndDevice")]
    EndDevice,

    [EnumMember(Value = "Router")]
    Router,

    [EnumMember(Value = "epoch")]
    Coordinator
}

[JsonConverter(typeof(StringEnumConverter))]
internal enum State
{
    [EnumMember(Value = "available")]
    Available,

    [EnumMember(Value = "updating")]
    Updating
}

internal class OTAState : BaseJson
{
    [JsonProperty("state")]
    public State State { get; set; }

    [JsonProperty("progress")]
    public float Progress { get; set; }

    [JsonProperty("remaining")]
    public float Remaining { get; set; }
}
#nullable restore