using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

[JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(CamelCaseNamingStrategy))]
public enum EditType { Button, RaisedButton, FloatingActionButton, IconButton, Toggle, Dropdown, Slider, Input, Icon }
