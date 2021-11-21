using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using AppBrokerASP.Devices.Painless.Heater;

namespace AppBrokerASP.Database.Model;

public class HeaterConfigModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public Devices.Painless.Heater.DayOfWeek DayOfWeek { get; set; }
    public DateTime TimeOfDay { get; set; }
    public double Temperature { get; set; }

    [ForeignKey("DeviceId")]
    public virtual DeviceModel? Device { get; set; }

    public static implicit operator HeaterConfig(HeaterConfigModel model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };

    public static implicit operator HeaterConfigModel(HeaterConfig model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };
}
