﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBrokerASP.Database.Model;

public class DeviceMappingModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [ForeignKey("ParentId")]
    public virtual DeviceModel? Parent { get; set; }
    [ForeignKey("ChildId")]
    public virtual DeviceModel? Child { get; set; }
}
