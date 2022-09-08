using Newtonsoft.Json.Linq;


namespace AppBrokerASP.Zigbee2Mqtt;

public record struct StateChangeArgs(long Id, string PropertyName, JToken? OldValue, JToken NewValue);
