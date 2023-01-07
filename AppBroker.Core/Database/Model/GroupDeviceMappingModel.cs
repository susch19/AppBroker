
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

public class GroupDeviceMappingModel
{
    [Key, Column(Order = 0)]
    public long DeviceId { get; set; }

    [Key, Column(Order = 1)]
    public int GroupId { get; set; }
}