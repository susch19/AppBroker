using AppBroker.Core;
using AppBroker.Core.Models;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace AppBrokerASP.Zigbee2Mqtt;


public class HistoryManager : IHistoryManager
{
    static HistoryManager()
    {
        using var ctx = new HistoryContext();
        ctx.Database.Migrate();
    }

    public void StoreNewState(long id, string name, JToken? oldValue, JToken? newValue)
    {
        if (newValue is null)
            return;
        using var ctx = new HistoryContext();

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
            default:
                return;
        }
        value.Timestamp = DateTime.UtcNow;
        histProp.Values.Add(value);

        ctx.SaveChanges();
    }

    public void EnableHistory(long id, string name)
    {
        using var ctx = new HistoryContext();
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
        using var ctx = new HistoryContext();
        var histProp = ctx.Properties
            .FirstOrDefault(x => x.PropertyName == name && x.Enabled == true && x.Device.DeviceId == id);
        if (histProp is null || !histProp.Enabled)
            return;
        histProp.Enabled = false;
        ctx.SaveChanges();
    }

    public List<HistoryPropertyState> GetHistoryProperties()
    {
        using var ctx = new HistoryContext();
        return ctx.Properties.Include(x => x.Device).Select(x => new HistoryPropertyState(x.Device.DeviceId, x.PropertyName, x.Enabled)).ToList();
    }


    public History.HistoryRecord[] GetHistoryFor(long deviceId, string propertyName, DateTimeOffset start, DateTimeOffset end)
    {
        using var ctx = new HistoryContext();

        var histProp = ctx.Properties.FirstOrDefault(x => x.PropertyName == propertyName && x.Device.DeviceId == deviceId);
        if (histProp == default)
            return Array.Empty<History.HistoryRecord>();

        var values = histProp.Values
            .Where(x => x.Timestamp > start && x.Timestamp < end)
            .ToArray();
        return values
            .OfType<HistoryValueDouble>()
            .Select(x => new History.HistoryRecord(x.Value, (long)(new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds)))
            .Concat(values.OfType<HistoryValueBool>().Select(x => new History.HistoryRecord(x.Value ? 1d : 0d, (long)(new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds))))
            .Concat(values.OfType<HistoryValueLong>().Select(x => new History.HistoryRecord(x.Value, (long)(new TimeSpan(x.Timestamp.Ticks).TotalMilliseconds))))
            .OrderBy(x => x.Ts)
            .ToArray();
    }

    public class HistoryDevice
    {

        [Key]
        public int Id { get; set; }

        public long DeviceId { get; set; }

        public virtual ICollection<HistoryProperty> HistoryValues { get; set; }

    }
    public class HistoryProperty
    {
        [Key]
        public long Id { get; set; }
        public bool Enabled { get; set; }
        public string PropertyName { get; set; }


        public virtual HistoryDevice Device { get; set; }
        public virtual ICollection<HistoryValueBase> Values { get; set; }

        public IEnumerable<T> GetValues<T>() where T : HistoryValueBase
        {
            return Values.OfType<T>();
        }
    }

    [Table("HistoryValueBase")]
    public class HistoryValueBase
    {
        [Key]
        public long Id { get; set; }
        public DateTime Timestamp { get; set; }

        //TODO Add retentionpolicy
        //TODO Add past concation? (Example after 1 Month group values for each 10 Minutes into one, for devices which have a lot of state changes)

        public virtual HistoryProperty HistoryValue { get; set; }
    }

    [Table("HistoryValueLong")]
    public class HistoryValueLong : HistoryValueBase
    {
        public long Value { get; set; }

        public HistoryValueLong()
        {
        }
        public HistoryValueLong(long value)
        {
            Value = value;
        }
    }

    [Table("HistoryValueString")]
    public class HistoryValueString : HistoryValueBase
    {
        public string Value { get; set; }

        public HistoryValueString()
        {
        }
        public HistoryValueString(string value)
        {
            Value = value;
        }
    }

    [Table("HistoryValueDouble")]
    public class HistoryValueDouble : HistoryValueBase
    {
        public double Value { get; set; }

        public HistoryValueDouble()
        {
        }
        public HistoryValueDouble(double value)
        {
            Value = value;
        }
    }

    [Table("HistoryValueBool")]
    public class HistoryValueBool : HistoryValueBase
    {
        public bool Value { get; set; }

        public HistoryValueBool()
        {
        }
        public HistoryValueBool(bool value)
        {
            Value = value;
        }
    }
    [Table("HistoryValueDateTime")]
    public class HistoryValueDateTime : HistoryValueBase
    {
        public DateTime Value { get; set; }

        public HistoryValueDateTime()
        {
        }
        public HistoryValueDateTime(DateTime value)
        {
            Value = value;
        }
    }
    [Table("HistoryValueTimeSpan")]
    public class HistoryValueTimeSpan : HistoryValueBase
    {
        public TimeSpan Value { get; set; }

        public HistoryValueTimeSpan()
        {
        }
        public HistoryValueTimeSpan(TimeSpan value)
        {
            Value = value;
        }
    }
    internal class HistoryContext : DbContext
    {
        public DbSet<HistoryDevice> Devices { get; set; }
        public DbSet<HistoryProperty> Properties { get; set; }
        public DbSet<HistoryValueBase> ValueBases { get; set; }

        public DbSet<HistoryValueBool> HistoryBools { get; set; }
        public DbSet<HistoryValueString> HistoryStrings { get; set; }
        public DbSet<HistoryValueDouble> HistoryDoubles { get; set; }
        public DbSet<HistoryValueLong> HistoryLongs { get; set; }
        public DbSet<HistoryValueDateTime> HistoryDates { get; set; }
        public DbSet<HistoryValueTimeSpan> HistoryTimespans { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder
                .UseSqlite("Data Source=history.db")
                .UseLazyLoadingProxies();
        }
    }
}
