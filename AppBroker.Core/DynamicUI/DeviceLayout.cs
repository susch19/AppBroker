using Newtonsoft.Json;

namespace AppBroker.Core.DynamicUI;


public record DeviceLayout(string UniqueName, string? TypeName, List<long>? Ids, DashboardDeviceLayout? DashboardDeviceLayout, DetailDeviceLayout? DetailDeviceLayout);
