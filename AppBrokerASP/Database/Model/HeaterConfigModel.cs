using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using AppBroker.Core.Models;

using AppBrokerASP.Devices.Painless.Heater;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBrokerASP.Database.Model;

public class HeaterConfigModel : IHeaterConfigModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [ForeignKey("DeviceId")]
    public virtual DeviceModel? Device { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime TimeOfDay { get; set; }
    public double Temperature { get; set; }

    public static implicit operator HeaterConfig(HeaterConfigModel model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };

    public static implicit operator HeaterConfigModel(HeaterConfig model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };
}
