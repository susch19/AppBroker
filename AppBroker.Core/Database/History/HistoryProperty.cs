using System.ComponentModel.DataAnnotations;

namespace AppBroker.Core.Database.History;

public class HistoryProperty
{
    [Key]
    public long Id { get; set; }
    public bool Enabled { get; set; }
    public string PropertyName { get; set; }


    public virtual HistoryDevice Device { get; set; }
    public virtual ICollection<HistoryValueBase> Values { get; set; }

    public IEnumerable<T> GetValues<T>() where T : HistoryValueBase
    {
        return Values.OfType<T>();
    }
}
