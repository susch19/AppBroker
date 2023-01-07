using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using AppBroker.Core.Models;

using Newtonsoft.Json;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBroker.Core.Database.Model;

public class HeaterConfigModel : IHeaterConfigModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
    public long DeviceId{ get; set; }
    public int HeatingPlanId { get; set; }
    
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime TimeOfDay { get; set; }
    public double Temperature { get; set; }

    [ForeignKey(nameof(DeviceId))]
    public virtual DeviceModel? Device { get; set; }
    [ForeignKey(nameof(HeatingPlanId))]
    public virtual HeatingPlanModel? HeatingPlan { get; set; }

    public static implicit operator HeaterConfig(HeaterConfigModel model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };

    public static implicit operator HeaterConfigModel(HeaterConfig model) => new() { DayOfWeek = model.DayOfWeek, Temperature = model.Temperature, TimeOfDay = model.TimeOfDay };
}
