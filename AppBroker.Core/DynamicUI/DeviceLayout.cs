using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NSwag.Annotations;

namespace AppBroker.Core.DynamicUI;


public record DeviceLayout(
    string UniqueName, 
    string IconName,
    string? TypeName,
    string[]? TypeNames,
    long[]? Ids, 
    DashboardDeviceLayout? DashboardDeviceLayout, 
    DetailDeviceLayout? DetailDeviceLayout,
    List<NotificationSetup>? NotificationSetup,
    [property:Newtonsoft.Json.JsonExtensionData, OpenApiIgnore] Dictionary<string, JToken> AdditionalDataDes,

    int Version = 1, 
    bool ShowOnlyInDeveloperMode = false,
    string Hash = "")
{
    public Dictionary<string, JToken> AdditionalData => AdditionalDataDes;
} 


public record NotificationSetup(string UniqueName, string TranslatableName, int Times = -1, List<long>? DeviceIds = null, bool Global = false);