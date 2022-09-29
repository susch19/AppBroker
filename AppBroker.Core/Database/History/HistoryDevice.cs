using System.ComponentModel.DataAnnotations;

namespace AppBroker.Core.Database.History;

public class HistoryDevice
{

    [Key]
    public int Id { get; set; }

    public long DeviceId { get; set; }

    public virtual ICollection<HistoryProperty> HistoryValues { get; set; }

}
