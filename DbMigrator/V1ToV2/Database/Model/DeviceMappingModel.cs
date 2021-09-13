﻿using AppBrokerASP.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DbMigrator.V1ToV2.Database.Model
{
    public class DeviceMappingModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [ForeignKey("ParentId")]
        public DeviceModel Parent { get; set; }
        [ForeignKey("ChildId")]
        public DeviceModel Child { get; set; }
    }
}
