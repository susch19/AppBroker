using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Core.DynamicUI;


public record DeviceLayout(
    string UniqueName, 
    string? TypeName,
    string[]? TypeNames,
    long[]? Ids, 
    DashboardDeviceLayout? DashboardDeviceLayout, 
    DetailDeviceLayout? DetailDeviceLayout, 
    [property:Newtonsoft.Json.JsonExtensionData]IDictionary<string, JToken> AdditionalData,
    int Version = 1, 
    bool ShowOnlyInDeveloperMode = false ); 
