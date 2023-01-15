using Newtonsoft.Json.Linq;

namespace AppBroker.Core;
public record struct StateChangeArgs(long Id, string PropertyName, JToken? OldValue, JToken NewValue);


[Flags]
public enum StateFlags
{
    None = 0,
    NotifyOfStateChange = 1 << 0,
    SendDataToApp = 1 << 1,
    StoreLastState = 1 << 2,
    SendToThirdParty = 1 << 3,

    AllExceptThirdParty = 0x7FFFFFFF ^ SendToThirdParty,
    All = 0x7FFFFFFF

}
public interface IDeviceStateManager
{
    event EventHandler<StateChangeArgs>? StateChanged;

    Dictionary<string, JToken>? GetCurrentState(long id);
    object? GetSingleStateValue(long id, string propertyName);
    JToken? GetSingleState(long id, string propertyName);
    bool ManagesDevice(long id);
    /// <summary>
    /// Sets multiple states for the device with the property names. 
    /// Missing states will be created. The state may be stored in the db afterwards.
    /// If no state change was detected (old values equals new values), nothing will happen
    /// </summary>
    /// <param name="id">Id of the device for the state</param>
    /// <param name="newState">The Dictionary composed of Property Name and the new State Value</param>
    /// <param name="stateFlags">Can be used to dictate what should happen with the state</param>
    void SetMultipleStates(long id, Dictionary<string, JToken> newState, StateFlags stateFlags = StateFlags.AllExceptThirdParty);
    /// <summary>
    /// Sets a state for the device with the property name. If the state is not present it will be created. The state will be stored in the db afterwards.
    /// If no state change was detected (old value equals new value), nothing will happen
    /// </summary>
    /// <param name="id">Id of the device for the state</param>
    /// <param name="propertyName">The name of the state</param>
    /// <param name="newVal">The value that the state should have</param>
    /// <param name="stateFlags">Can be used to dictate what should happen with the state</param>
    void SetSingleState(long id, string propertyName, JToken newVal, StateFlags stateFlags = StateFlags.AllExceptThirdParty);
    bool TryGetCurrentState(long id, out Dictionary<string, JToken>? result);
    string MapValueName(long id, string name);
}