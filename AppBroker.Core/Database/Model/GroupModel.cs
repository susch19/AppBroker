using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

public class GroupModel
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }


    [InverseProperty(nameof(GroupDeviceMappingModel.GroupId))]
    public ICollection<GroupDeviceMappingModel> DeviceGroupMappings { get; set; }

}