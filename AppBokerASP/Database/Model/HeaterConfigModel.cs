using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using AppBokerASP.Devices.Painless.Heater;

namespace AppBokerASP.Database.Model
{
    public class HeaterConfigModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public Devices.Painless.Heater.DayOfWeek DayOfWeek { get; set; }
        public DateTime TimeOfDay { get; set; }
        public double Temperature { get; set; }
        
        [ForeignKey("DeviceId")]
        public virtual DeviceModel Device { get; set; }

        public static implicit operator HeaterConfig(HeaterConfigModel model) => new HeaterConfig {DayOfWeek = model.DayOfWeek,  Temperature = model.Temperature, TimeOfDay = model.TimeOfDay};

        public static implicit operator HeaterConfigModel(HeaterConfig model) => new HeaterConfigModel { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };
    }
}
