using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace AppBroker.Core.Database.History;

[Table("HistoryValueBase")]
public class HistoryValueBase
{
    [Key]
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public long HistoryValueId { get; set; }

    //TODO Add retentionpolicy
    //TODO Add past concation? (Example after 1 Month group values for each 10 Minutes into one, for devices which have a lot of state changes)

    [ForeignKey(nameof(HistoryValueId))]
    public virtual HistoryProperty HistoryValue { get; set; }
}
