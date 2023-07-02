using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

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
