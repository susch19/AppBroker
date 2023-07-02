using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
