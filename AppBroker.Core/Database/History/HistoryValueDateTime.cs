using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
