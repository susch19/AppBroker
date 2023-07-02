using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

[Table("HistoryProperties")]
public class HistoryProperty
{
    [Key]
    public long Id { get; set; }
    public bool Enabled { get; set; }
    public string PropertyName { get; set; }

    public int DeviceId { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public virtual HistoryDevice Device { get; set; }
    public virtual ICollection<HistoryValueBase> Values { get; set; }

    public IEnumerable<T> GetValues<T>() where T : HistoryValueBase
    {
        return Values.OfType<T>();
    }
}
