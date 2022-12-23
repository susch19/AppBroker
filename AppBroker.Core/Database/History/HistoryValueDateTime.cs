using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
