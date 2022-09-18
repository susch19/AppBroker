using System.Runtime.CompilerServices;

using DayOfWeek = IoBrokerHistoryImporter.DayOfWeek;

namespace IoBrokerHistoryImporter;

public interface IHeaterConfigModel
{
    DayOfWeek DayOfWeek { get; set; }
    DateTime TimeOfDay { get; set; }
    double Temperature { get; set; }
}
