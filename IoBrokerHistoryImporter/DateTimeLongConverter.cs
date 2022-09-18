using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IoBrokerHistoryImporter;

public class DateTimeLongConverter : ValueConverter<DateTime, long>
{
    public DateTimeLongConverter()
        : base(
            v => v.Ticks,
            v => new DateTime(v))
    {
    }
}

public class TimeSpanLongConverter : ValueConverter<TimeSpan, long>
{
    public TimeSpanLongConverter()
        : base(
            v => v.Ticks,
            v => new TimeSpan(v))
    {
    }
}
