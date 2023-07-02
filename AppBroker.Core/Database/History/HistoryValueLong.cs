using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
