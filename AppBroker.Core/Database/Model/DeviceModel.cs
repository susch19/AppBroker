using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

public class DeviceModel
{
    [Key]
    public long Id { get; set; }
    public string TypeName { get; set; } = "";
    public string? FriendlyName { get; set; }
    public string? FriendlyUniqueName { get; set; }

    public string? LastState { get; set; }
    public DateTime? LastStateChange { get; set; }
    public bool StartAutomatically { get; set; }
    public string? DeserializationData { get; set; }


    [InverseProperty(nameof(GroupDeviceMappingModel.Device))]
    public virtual ICollection<GroupDeviceMappingModel> DeviceGroupMappings { get; set; }

    [InverseProperty(nameof(HeaterConfigModel.Device))]
    public virtual ICollection<HeaterConfigModel>? HeaterConfigs { get; set; }
}
