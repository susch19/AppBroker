using AppBroker.Core.Models;

using Newtonsoft.Json.Linq;

namespace AppBroker.Core;

public record struct HistoryPropertyState(long DeviceId, string PropertyName, bool Enabled);

public record struct InMemoryHistoryState(DateTime CreateDate, JToken Value);
public interface IHistoryManager
{
    void DisableHistory(long id, string name);
    void EnableHistory(long id, string name);
    HistoryRecord[] GetHistoryFor(long deviceId, string propertyName, DateTime start, DateTime end);
    List<HistoryPropertyState> GetHistoryProperties();
    Queue<InMemoryHistoryState>? GetInMemoryHistory(long deviceId, string propertyName);
    Queue<InMemoryHistoryState>? GetInMemoryHistory(long deviceId, string propertyName, DateTime start);
    Queue<InMemoryHistoryState>? GetInMemoryHistory(long deviceId, string propertyName, DateTime start, DateTime end);
    void StoreNewState(long id, string name, JToken? oldValue, JToken? newValue);
}