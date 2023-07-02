using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

[Table("Devices")]
public class DeviceModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }
    public string TypeName { get; set; } = "";
    public string? FriendlyName { get; set; }
    public string? LastState { get; set; }
    public DateTime? LastStateChange { get; set; }
    public bool StartAutomatically { get; set; }
    public string? DeserializationData { get; set; }

    [InverseProperty(nameof(HeaterConfigModel.Device))]
    public virtual ICollection<HeaterConfigModel>? HeaterConfigs { get; set; }
}
