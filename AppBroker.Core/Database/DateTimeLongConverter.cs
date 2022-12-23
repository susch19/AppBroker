using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AppBroker.Core.Database;

public class DateTimeLongConverter : ValueConverter<DateTime, long>
{
    public DateTimeLongConverter()
        : base(
            v => v.Ticks,
            v => new DateTime(v))
    {
    }
}
