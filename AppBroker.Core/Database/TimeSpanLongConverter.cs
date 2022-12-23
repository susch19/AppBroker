using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AppBroker.Core.Database;

public class TimeSpanLongConverter : ValueConverter<TimeSpan, long>
{
    public TimeSpanLongConverter()
        : base(
            v => v.Ticks,
            v => new TimeSpan(v))
    {
    }
}
