using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using AppBokerASP.Devices.Heater;

namespace DbMigrator.V1ToV2.Database.Model
{
    public class HeaterConfigModelOld
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public AppBokerASP.Devices.Heater.DayOfWeek DayOfWeek { get; set; }
        public DateTime TimeOfDay { get; set; }
        public double Temperature { get; set; }

        [ForeignKey("DeviceId")]
        public virtual DeviceModel Device { get; set; }

    }
}
