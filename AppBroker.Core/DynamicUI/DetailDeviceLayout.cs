namespace AppBroker.Core.DynamicUI;

public record DetailDeviceLayout(
    List<DetailPropertyInfo> PropertyInfos,
    List<DetailTabInfo> TabInfos,
    List<HistoryPropertyInfo> HistoryProperties);



//public record EditParameter([JsonConverter(typeof(StringEnumConverter))] Command Command, JToken Value, int? Id = null, MessageType? MessageType = null, string? DisplayName = null, List<JToken>? Parameters = null, [property: JsonExtensionData] Dictionary<string, JToken>? ExtensionData = null);

//public record DetailPropertyInfo(string Name, int Order, int? RowNr = null, string DisplayName = "", string UnitOfMeasurement = "", string Format = "", PropertyEditInformation? EditInfo = null, TextStyle? TextStyle = null, int TabInfoId = 0, SpecialDetailType SpecialType = SpecialDetailType.None, bool? ShowOnlyInDeveloperMode = null);

//public record DetailTabInfo(int Id, string IconName, int Order, LinkedDeviceTab? LinkedDevice = null);  // Have another device in a tab, like Heater and Temperature Sensor
//public record LinkedDeviceTab(string DeviceIdPropertyName, string DeviceType);
//public record PropertyEditInformation(MessageType EditCommand, List<EditParameter> EditParameter, EditType EditType, string? Display, string? HubMethod, string? ValueName, object? ActiveValue, [property: JsonExtensionData] Dictionary<string, JToken>? ExtensionData);

//public record DetailDeviceLayout(List<DetailPropertyInfo> PropertyInfos, List<DetailTabInfo> TabInfos, List<HistoryPropertyInfo> HistoryProperties);