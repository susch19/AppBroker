using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

[JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(CamelCaseNamingStrategy))]
public enum SpecialDetailType
{
    None = 0,
    Target = 1,
    Current = 2,
}

public record DetailPropertyInfo(string Name, int Order, string DisplayName = "", string UnitOfMeasurement = "", string Format = "", PropertyEditInformation? EditInfo = null, TextStyle? TextStyle = null, int TabInfoId = 0, SpecialDetailType SpecialType = SpecialDetailType.None, bool? ShowOnlyInDeveloperMode = null);
public record DetailTabInfo(int Id, string IconName, int Order, LinkedDeviceTab? LinkedDevice = null);  // Have another device in a tab, like Heater and Temperature Sensor
public record LinkedDeviceTab(string DeviceIdPropertyName, string DeviceType);
public record PropertyEditInformation(MessageType EditType, Command EditCommand);

public record DetailDeviceLayout(List<DetailPropertyInfo> PropertyInfos, List<DetailTabInfo> TabInfos, List<HistoryPropertyInfo> HistoryProperties);
