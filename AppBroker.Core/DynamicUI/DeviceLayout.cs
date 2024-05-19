using Newtonsoft.Json;

namespace AppBroker.Core.DynamicUI;


public record DeviceLayout(
    string UniqueName, 
    string? TypeName,
    string[]? TypeNames,
    long[]? Ids, 
    DashboardDeviceLayout? DashboardDeviceLayout, 
    DetailDeviceLayout? DetailDeviceLayout, 
    [property:Newtonsoft.Json.JsonExtensionData]IDictionary<string, string> AdditionalData,
    int Version = 1, 
    bool ShowOnlyInDeveloperMode = false ); 
