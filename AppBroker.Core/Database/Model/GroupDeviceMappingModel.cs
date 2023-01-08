
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

[PrimaryKey(nameof(DeviceId), nameof(GroupId))]
public class GroupDeviceMappingModel
{
    public long DeviceId { get; set; }

    public int GroupId { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public virtual DeviceModel Device { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual GroupModel Group { get; set; }
}