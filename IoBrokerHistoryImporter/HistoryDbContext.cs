using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IoBrokerHistoryImporter;

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

[Table("HistoryValueBases")]
public class HistoryValueBase
{
    [Key]
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }

    //TODO Add retentionpolicy
    //TODO Add past concation? (Example after 1 Month group values for each 10 Minutes into one, for devices which have a lot of state changes)

    public virtual HistoryProperty HistoryValue { get; set; }
}

[Table("HistoryValueLongs")]
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

[Table("HistoryValueStrings")]
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

[Table("HistoryValueDoubles")]
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

[Table("HistoryValueBools")]
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
[Table("HistoryValueDateTimes")]
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
[Table("HistoryValueTimeSpans")]
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
[Table("HistoryValueHeaterConfigs")]
public class HistoryValueHeaterConfig : HistoryValueBase, IHeaterConfigModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime TimeOfDay { get; set; }
    public double Temperature { get; set; }

    public HistoryValueHeaterConfig()
    {
    }
    public HistoryValueHeaterConfig(DayOfWeek dayOfWeek, DateTime timeOfDay, double temperature)
    {
        DayOfWeek = dayOfWeek;
        TimeOfDay = timeOfDay;
        Temperature = temperature;

    }
    public HistoryValueHeaterConfig(IHeaterConfigModel configModel)
    {
        DayOfWeek = configModel.DayOfWeek;
        TimeOfDay = configModel.TimeOfDay;
        Temperature = configModel.Temperature;
    }
}
internal class HistoryContext : DbContext
{
    private readonly string path;

    public DbSet<HistoryDevice> Devices { get; set; }
    public DbSet<HistoryProperty> Properties { get; set; }
    public DbSet<HistoryValueBase> ValueBases { get; set; }

    public DbSet<HistoryValueBool> HistoryBools { get; set; }
    public DbSet<HistoryValueString> HistoryStrings { get; set; }
    public DbSet<HistoryValueDouble> HistoryDoubles { get; set; }
    public DbSet<HistoryValueLong> HistoryLongs { get; set; }
    public DbSet<HistoryValueDateTime> HistoryDates { get; set; }
    public DbSet<HistoryValueTimeSpan> HistoryTimespans { get; set; }
    public DbSet<HistoryValueHeaterConfig> HistoryHeaterConfigs { get; set; }

    public HistoryContext(string path)
    {
        this.path = path;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder
            .UseSqlite("Data Source=" + path)
            .UseLazyLoadingProxies();
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeLongConverter>();
        configurationBuilder
            .Properties<TimeSpan>()
            .HaveConversion<TimeSpanLongConverter>();
    }
}