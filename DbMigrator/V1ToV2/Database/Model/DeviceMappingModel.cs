using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbMigrator.V1ToV2.Database.Model;

public class DeviceMappingModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [ForeignKey("ParentId")]
    public DeviceModel Parent { get; set; }
    [ForeignKey("ChildId")]
    public DeviceModel Child { get; set; }
}
