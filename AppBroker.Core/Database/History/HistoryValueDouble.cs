using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
