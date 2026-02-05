using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBroker.Plugins;

public interface IDevice
{
    long Id { get; set; }
    bool StartAutomatically { get; set; }
    string TypeName { get; set; }
    List<string> TypeNames { get; }
}