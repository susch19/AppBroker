using AppBroker.Core.Models;

using System.ComponentModel.DataAnnotations.Schema;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBroker.Core.Database.History;

[Table("HistoryValueHeaterConfigs")]
public class HistoryValueHeaterConfig : HistoryValueBase, IHeaterConfigModel
{
    public DayOfWeek DayOfWeek { get; set; }
    public DateTime TimeOfDay { get; set; }
    public double Temperature { get; set; }

    public HistoryValueHeaterConfig()
    {
    }
    public HistoryValueHeaterConfig(DayOfWeek dayOfWeek, DateTime timeOfDay, double temperature)
    {
        DayOfWeek = dayOfWeek;
        TimeOfDay = timeOfDay;
        Temperature = temperature;
    }
    public HistoryValueHeaterConfig(IHeaterConfigModel configModel)
    {
        DayOfWeek = configModel.DayOfWeek;
        TimeOfDay = configModel.TimeOfDay;
        Temperature = configModel.Temperature;
    }
}
