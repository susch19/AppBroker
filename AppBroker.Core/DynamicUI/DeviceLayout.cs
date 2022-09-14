using Newtonsoft.Json;

namespace AppBroker.Core.DynamicUI;


public record DeviceLayout(
    string UniqueName, 
    string? TypeName,
    string[]? TypeNames,
    long[]? Ids, 
    DashboardDeviceLayout? DashboardDeviceLayout, 
    DetailDeviceLayout? DetailDeviceLayout, 
    int Version = 1, 
    bool ShowOnlyInDeveloperMode = false);
