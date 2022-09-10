using Newtonsoft.Json.Linq;

namespace AppBrokerASP;
public record struct StateChangeArgs(long Id, string PropertyName, JToken? OldValue, JToken NewValue);

public interface IDeviceStateManager
{
    event EventHandler<StateChangeArgs>? StateChanged;

    Dictionary<string, JToken>? GetCurrentState(long id);
    object? GetSingleStateValue(long id, string propertyName);
    JToken? GetSingleState(long id, string propertyName);
    bool ManagesDevice(long id);
    void PushNewState(long id, Dictionary<string, JToken> newState);
    void SetSingleState(long id, string propertyName, JToken newVal);
    bool TryGetCurrentState(long id, out Dictionary<string, JToken>? result);
}