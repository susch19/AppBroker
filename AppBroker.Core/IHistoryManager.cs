using Newtonsoft.Json.Linq;

namespace AppBrokerASP.Zigbee2Mqtt;
public interface IHistoryManager
{
    void DisableHistory(long id, string name);
    void EnableHistory(long id, string name);
    void StoreNewState(long id, string name, JToken? oldValue, JToken? newValue);
}