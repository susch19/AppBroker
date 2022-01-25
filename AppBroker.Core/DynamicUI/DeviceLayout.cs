namespace AppBroker.Core.DynamicUI;

public record BasePropertyLayout(string Name, int Order, TextStyle? TextStyle = null, string UnitOfMeasurement = "", PropertyEditInformation? EditInfo = null);

public record DeviceLayout(string UniqueName, string? TypeName, List<long>? Ids, DashboardDeviceLayout? DashboardDeviceLayout, DetailDeviceLayout? DetailDeviceLayout);
