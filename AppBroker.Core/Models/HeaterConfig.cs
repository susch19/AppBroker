using AppBroker.Core.Models;

using Newtonsoft.Json;

using System.Runtime.CompilerServices;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBroker.Core.Models;

public interface IHeaterConfigModel
{
    DayOfWeek DayOfWeek { get; set; }
    DateTime TimeOfDay { get; set; }
    double Temperature { get; set; }
}

[AppBroker.ClassPropertyChangedAppbroker]
public partial class HeaterConfig : IHeaterConfigModel, IEquatable<HeaterConfig?>
{
    [property: JsonProperty("dayOfWeek"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    private DayOfWeek dayOfWeek;
    [property: JsonProperty("timeOfDay")]
    private DateTime timeOfDay;
    [property: JsonProperty("temperature")]
    private double temperature;

    public HeaterConfig()
    {

    }
    public HeaterConfig(DayOfWeek dayOfWeek, DateTime timeOfDay, double temperature)
    {
        DayOfWeek = dayOfWeek;
        TimeOfDay = timeOfDay;
        Temperature = temperature;
    }

    public static implicit operator TimeTempMessageLE(HeaterConfig hc)
        => new(hc.DayOfWeek, new TimeSpan(hc.TimeOfDay.Hour, hc.TimeOfDay.Minute, 0), (float)hc.Temperature);
    public static implicit operator HeaterConfig(TimeTempMessageLE ttm)
    {
        var dt = DateTime.Now;
        dt = dt.AddHours(ttm.Time.Hours - dt.Hour);
        dt = dt.AddMinutes(ttm.Time.Minutes - dt.Minute);
        return new HeaterConfig(ttm.DayOfWeek, dt, ttm.Temp);
    }

    public static bool operator ==(HeaterConfig? left, HeaterConfig? right) => EqualityComparer<HeaterConfig>.Default.Equals(left, right);
    public static bool operator !=(HeaterConfig? left, HeaterConfig? right) => !(left == right);

    protected virtual void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "") =>
        //WorkflowPropertySignaler.PropertyChanged(value, field, propertyName!);
        field = value;
    public override bool Equals(object? obj) => Equals(obj as HeaterConfig);
    public bool Equals(HeaterConfig? other) => other is not null && DayOfWeek == other.DayOfWeek && TimeOfDay == other.TimeOfDay && Temperature == other.Temperature;
    public override int GetHashCode() => HashCode.Combine(DayOfWeek, TimeOfDay, Temperature);
}
