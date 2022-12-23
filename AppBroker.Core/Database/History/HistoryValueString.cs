using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
