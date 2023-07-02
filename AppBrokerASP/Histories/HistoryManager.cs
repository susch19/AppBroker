using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Core.Database.History;
using AppBroker.Core.Models;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBrokerASP.Histories;


public class HistoryManager : IHistoryManager
{
    static HeaterConfig emptyHeaderConfig = new();


    public void StoreNewState(long id, string name, JToken? oldValue, JToken? newValue)
    {
        if (newValue is null)
            return;
        using var ctx = DbProvider.HistoryContext;

        var histProp = ctx.Properties
            .FirstOrDefault(x => x.PropertyName == name && x.Enabled == true && x.Device.DeviceId == id);
        if (histProp == default)
            return;

        HistoryValueBase value;
        switch (newValue.Type)
        {
            case JTokenType.Integer:
                value = new HistoryValueLong(newValue.Value<long>());
                break;
            case JTokenType.Float:
                value = new HistoryValueDouble(newValue.Value<double>());
                break;
            case JTokenType.String:
            case JTokenType.Guid:
            case JTokenType.Uri:
                value = new HistoryValueString(newValue.Value<string>());
                break;
            case JTokenType.Boolean:
                value = new HistoryValueBool(newValue.Value<bool>());
                break;
            case JTokenType.Date:
                value = new HistoryValueDateTime(newValue.Value<DateTime>());
                break;
            case JTokenType.TimeSpan:
                value = new HistoryValueTimeSpan(newValue.Value<TimeSpan>());
                break;
            case JTokenType.Object:
                var hc = newValue.ToObject<HeaterConfig>();
                if (hc is not null && hc != emptyHeaderConfig)
                    value = new HistoryValueHeaterConfig(hc);
                else
                {
                    return;
                }
                break;
            default:
                return;
        }
        value.Timestamp = DateTime.UtcNow;
        value.HistoryValue = histProp;
        ctx.Add(value);

        ctx.SaveChanges();
    }

    public void EnableHistory(long id, string name)
    {
        using var ctx = DbProvider.HistoryContext;
        var histProp = ctx.Properties
            .FirstOrDefault(x => x.PropertyName == name && x.Device.DeviceId == id);
        if (histProp is null)
        {
            var device = ctx.Devices.FirstOrDefault(x => x.DeviceId == id);
            device ??= ctx.Devices.Add(new HistoryDevice() { DeviceId = id }).Entity;
            histProp = ctx.Properties
                .Add(new HistoryProperty { Enabled = true, PropertyName = name, Device = device })
                .Entity;
        }
        else if (histProp.Enabled)
            return;
        histProp.Enabled = true;
        ctx.SaveChanges();
    }

    public void DisableHistory(long id, string name)
    {
        using var ctx = DbProvider.HistoryContext;
        var histProp = ctx.Properties
            .FirstOrDefault(x => x.PropertyName == name && x.Enabled == true && x.Device.DeviceId == id);
        if (histProp is null || !histProp.Enabled)
            return;
        histProp.Enabled = false;


        ctx.SaveChanges();
    }

    public List<HistoryPropertyState> GetHistoryProperties()
    {
        using var ctx = DbProvider.HistoryContext;
        return ctx.Properties.Include(x => x.Device).Select(x => new HistoryPropertyState(x.Device.DeviceId, x.PropertyName, x.Enabled)).ToList();
    }


    Random random = new Random();
    public HistoryRecord[] GetHistoryFor(long deviceId, string propertyName, DateTime start, DateTime end)
    {
        //return Enumerable.Range(0, 100).Select(x => new HistoryRecord(random.NextDouble() * 30, (long)new TimeSpan(start.AddMinutes((long)(5 * x)).Ticks).TotalMilliseconds)).ToArray();

        using var ctx = DbProvider.HistoryContext;

        var histProp = ctx.Properties.FirstOrDefault(x => x.PropertyName == propertyName && x.Device.DeviceId == deviceId);
        if (histProp == default)
            return Array.Empty<HistoryRecord>();

        var values = ctx.ValueBases
            .Where(x => x.HistoryValueId == histProp.Id
                && x.Timestamp > start.ToUniversalTime()
                && x.Timestamp < end.ToUniversalTime())
             .ToArray();
        
        return values
            .OfType<HistoryValueDouble>()
            .Select(x => new HistoryRecord(x.Value, (long)new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds))
            .Concat(values.OfType<HistoryValueBool>().Select(x => new HistoryRecord(x.Value ? 1d : 0d, (long)new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds)))
            .Concat(values.OfType<HistoryValueLong>().Select(x => new HistoryRecord(x.Value, (long)new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds)))
            .Concat(values.OfType<HistoryValueHeaterConfig>().Select(x => new HistoryRecord(x.Temperature, (long)new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds)))
            .OrderBy(x => x.Ts)
            .ToArray();
    }

}
