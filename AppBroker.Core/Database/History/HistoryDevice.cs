using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.History;

[Table("HistoryDevices")]
public class HistoryDevice
{

    [Key]
    public int Id { get; set; }

    public long DeviceId { get; set; }

    public virtual ICollection<HistoryProperty> HistoryValues { get; set; }

}
