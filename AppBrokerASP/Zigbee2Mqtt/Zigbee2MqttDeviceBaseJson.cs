using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Runtime.Serialization;

namespace AppBrokerASP.Zigbee2Mqtt;

#nullable disable
public abstract class Zigbee2MqttDeviceBaseJson
{
    [JsonExtensionData]
    public IDictionary<string, JToken> AdditionalData { get; set; }
}

public class Zigbee2MqttClusters : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("input")]
    public string[] Input { get; set; }

    [JsonProperty("output")]
    public string[] Output { get; set; }
}

public class Zigbee2MqttDeviceDefinition : Zigbee2MqttDeviceBaseJson
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
    public Zigbee2MqttGenericExposedFeature[] Exposes { get; set; }

    [JsonProperty("options")]
    public Zigbee2MqttGenericExposedFeature[] Options { get; set; }

    [JsonProperty("supports_ota")]
    public bool SupportsOta { get; set; }

    [JsonProperty("icon")]
    public string Icon { get; set; }
}

public class Zigbee2MqttReportingConfig : Zigbee2MqttDeviceBaseJson
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

public class Zigbee2MqttEndpointDescription : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("bindings")]
    public Zigbee2MqttBindRule[] Bindings { get; set; }

    [JsonProperty("configured_reportings")]
    public Zigbee2MqttReportingConfig[] ConfiguredReportings { get; set; }

    [JsonProperty("clusters")]
    public Zigbee2MqttClusters Clusters { get; set; }

    [JsonProperty("scenes")]
    public Zigbee2MqttScene[] Scenes { get; set; }
}

public class Zigbee2MqttScene : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("id")]
    public float Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }
}

public class Zigbee2MqttGroup : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("id")]
    public float Id { get; set; }

    [JsonProperty("members")]
    public Zigbee2MqttGroupAddress[] Members { get; set; }

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("scenes")]
    public Zigbee2MqttScene[] Scenes { get; set; }
}

[DebuggerDisplay("{FriendlyName} {ModelId}")]
public class Zigbee2MqttDeviceJson : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }

    [JsonProperty("type")]
    public Zigbee2MqttDeviceType Type { get; set; }

    [JsonProperty("network_address")]
    public float NetworkAddress { get; set; }

    [JsonProperty("power_source")]
    public Zigbee2MqttPowerSource PowerSource { get; set; }

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
    public Zigbee2MqttDeviceDefinition Definition { get; set; }

    [JsonProperty("date_code")]
    public string DateCode { get; set; }

    [JsonProperty("endpoints")]
    public Dictionary<string, Zigbee2MqttEndpointDescription> Endpoints { get; set; }

    [JsonProperty("friendly_name")]
    public string FriendlyName { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class Zigbee2MqttBindRule : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("cluster")]
    public string Cluster { get; set; }

    [JsonProperty("target")]
    public Zigbee2MqttTarget Target { get; set; }
}

public class Zigbee2MqttTarget : Zigbee2MqttDeviceBaseJson
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

public class Zigbee2MqttGroupAddress : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }

    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }
}

public class Zigbee2MqttMeta : Zigbee2MqttDeviceBaseJson
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

public class Zigbee2MqttCoordinator : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("channel")]
    public float Channel { get; set; }

    [JsonProperty("pan_id")]
    public float PanId { get; set; }

    [JsonProperty("extended_pan_id")]
    public float[] ExtendedPanId { get; set; }
}

public class Zigbee2MqttConfig : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("homeassistant")]
    public bool Homeassistant { get; set; }

    [JsonProperty("advanced")]
    public Zigbee2MqttAdvancedConfig Advanced { get; set; }

    [JsonProperty("devices")]
    public Dictionary<string, Dictionary<string, object>> Devices { get; set; }

    [JsonProperty("device_options")]
    public Dictionary<string, object> DeviceOptions { get; set; }
}

public class Zigbee2MqttBridgeConfig : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("commit")]
    public string Commit { get; set; }

    [JsonProperty("coordinator")]
    public Zigbee2MqttCoordinator Coordinator { get; set; }

    [JsonProperty("network")]
    public Zigbee2MqttNetwork Network { get; set; }

    [JsonProperty("log_level")]
    public string LogLevel { get; set; }

    [JsonProperty("permit_join")]
    public bool PermitJoin { get; set; }
}

public class Zigbee2MqttLogMessage
{
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("level")]
    public Zigbee2MqttLogLevel Level { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttLogLevel
{
    [EnumMember(Value = "error")]
    Error,

    [EnumMember(Value = "info")]
    Info,

    [EnumMember(Value = "warning")]
    Warning,
    [EnumMember(Value ="debug")]
    Debug,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttAvailabilityState
{
    [EnumMember(Value = "online")]
    Online,

    [EnumMember(Value = "offline")]
    Offline,
}

public class Zigbee2MqttBridgeInfo : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("config")]
    public Zigbee2MqttConfig Config { get; set; }

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
    public Zigbee2MqttCoordinator Coordinator { get; set; }

    [JsonProperty("device_options")]
    public Dictionary<string, object> DeviceOptions { get; set; }

    [JsonProperty("restart_required")]
    public bool RestartRequired { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttGenericFeatureType
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
public enum Zigbee2MqttFeatureAccessMode
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

public class FeatureJsonConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => typeof(Zigbee2MqttGenericExposedFeature).IsAssignableFrom(objectType);

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
            "numeric" => new Zigbee2MqttNumericFeature(),
            "binary" => new Zigbee2MqttBinaryFeature(),
            "enum" => new Zigbee2MqttEnumFeature(),
            "list" => new Zigbee2MqttListFeature(),
            "fan" => new Zigbee2MqttFanFeature(),
            "light" => new Zigbee2MqttLightFeature(),
            "switch" => new Zigbee2MqttSwitchFeature(),
            "cover" => new Zigbee2MqttCoverFeature(),
            "lock" => new Zigbee2MqttLockFeature(),
            "climate" => new Zigbee2MqttClimateFeature(),

            _ => Default(),
        };

        serializer.Populate(jObject.CreateReader(), targetType);

        return targetType;

        Zigbee2MqttGenericExposedFeature Default()
        {
            var name = jObject.Value<string>("name");
            return name switch
            {
                "color_hs" or "color_xy" => new Zigbee2MqttColorFeature(),
                _ => new Zigbee2MqttGenericExposedFeature()
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
public class Zigbee2MqttGenericExposedFeature : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("type")]
    public Zigbee2MqttGenericFeatureType Type { get; set; }


    [JsonProperty("name")]
    public string Name { get; set; }


    [JsonProperty("unit")]
    public string Unit { get; set; } = "string";


    [JsonProperty("access")]
    public Zigbee2MqttFeatureAccessMode Access { get; set; }


    [JsonProperty("endpoint")]
    public string Endpoint { get; set; }


    [JsonProperty("property")]
    public string Property { get; set; }


    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("features")]
    public Zigbee2MqttGenericExposedFeature[] Features { get; set; }

    public virtual bool ValidateValue(object value) => true;
}

public class Zigbee2MqttBinaryFeature : Zigbee2MqttGenericExposedFeature
{
    [JsonProperty("value_on")]
    public object ValueOn { get; set; }

    [JsonProperty("value_off")]
    public object ValueOff { get; set; }

    [JsonProperty("value_toggle")]
    public object ValueToggle { get; set; }

    public Zigbee2MqttBinaryFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Binary;
    }

    public JToken ConvertToBool(object v)
    {
        
        return string.Equals(v.ToString(), ValueOn.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

public class Zigbee2MqttListFeature : Zigbee2MqttGenericExposedFeature
{
    [JsonProperty("item_type")]
    public string ItemType { get; set; } = "number";

    public Zigbee2MqttListFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.List;
    }
}

public class Zigbee2MqttNumericFeaturePreset : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("value")]
    public float Value { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class Zigbee2MqttNumericFeature : Zigbee2MqttGenericExposedFeature
{
    [JsonProperty("value_min")]
    public float ValueMin { get; set; }

    [JsonProperty("value_max")]
    public float ValueMax { get; set; }

    [JsonProperty("value_step")]
    public float ValueStep { get; set; }

    [JsonProperty("presets")]
    public Zigbee2MqttNumericFeaturePreset[] Presets { get; set; }

    public Zigbee2MqttNumericFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Numeric;
    }
}

public class Zigbee2MqttTextualFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttTextualFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Text;
    }
}

public class Zigbee2MqttEnumFeature : Zigbee2MqttGenericExposedFeature
{
    [JsonProperty("values")]
    public object[] Values { get; set; }

    public Zigbee2MqttEnumFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Enum;
    }

    public override bool ValidateValue(object value) 
        => Values.Contains(value);
}

public class Zigbee2MqttLightFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttLightFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Light;
    }
}

public class Zigbee2MqttSwitchFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttSwitchFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Switch;
    }
}

public class Zigbee2MqttCoverFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttCoverFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Cover;
    }
}

public class Zigbee2MqttLockFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttLockFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Lock;
    }
}
public class Zigbee2MqttFanFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttFanFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Fan;
    }
}

public class Zigbee2MqttClimateFeature : Zigbee2MqttGenericExposedFeature
{
    public Zigbee2MqttClimateFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Climate;
    }
}

public class Zigbee2MqttColorFeature : Zigbee2MqttGenericExposedFeature
{
    [JsonProperty("name")]
    public new Zigbee2MqttColorName Name { get; set; }

    [JsonProperty("features")]
    public new Zigbee2MqttNumericFeature[] Features { get; set; }

    public Zigbee2MqttColorFeature()
    {
        Type = Zigbee2MqttGenericFeatureType.Composite;
    }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttColorName
{
    [EnumMember(Value = "color_xy")]
    ColorXY,

    [EnumMember(Value = "color_hs")]
    ColorHS,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttPowerSource
{
    [EnumMember(Value = "Battery")]
    Battery,

    [EnumMember(Value = "Mains (single phase)")]
    Mains,

    [EnumMember(Value = "DC Source")]
    DC,
}

public class Coordinator1 : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("meta")]
    public Zigbee2MqttMeta Meta { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("ieee_address")]
    public string IEEEAddress { get; set; }
}


public class Zigbee2MqttTouchLinkDevice : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("ieee_address")]
    public string IEEEEAddress { get; set; }

    [JsonProperty("channel")]
    public float Channel { get; set; }
}


[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttLastSeen
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

public class Zigbee2MqttAdvancedConfig : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("elapsed")]
    public bool Elapsed { get; set; }

    [JsonProperty("last_seen")]
    public Zigbee2MqttLastSeen LastSeen { get; set; }

    [JsonProperty("legacy_api")]
    public bool LegacyApi { get; set; }
}

public class Zigbee2MqttNetwork : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("meta")]
    public Zigbee2MqttMeta Meta { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttDeviceType
{
    [EnumMember(Value = "EndDevice")]
    EndDevice,

    [EnumMember(Value = "Router")]
    Router,

    [EnumMember(Value = "epoch")]
    Coordinator
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Zigbee2MqttState
{
    [EnumMember(Value = "available")]
    Available,

    [EnumMember(Value = "updating")]
    Updating
}

public class Zigbee2MqttOTAState : Zigbee2MqttDeviceBaseJson
{
    [JsonProperty("state")]
    public Zigbee2MqttState State { get; set; }

    [JsonProperty("progress")]
    public float Progress { get; set; }

    [JsonProperty("remaining")]
    public float Remaining { get; set; }
}
#nullable restore