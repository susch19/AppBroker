using Newtonsoft.Json;

using System.Runtime.CompilerServices;

namespace AppBrokerASP.Devices.Painless.Heater;

[AppBroker.ClassPropertyChangedAppbroker]
public partial class HeaterConfig
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

    protected virtual void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "") =>
        //WorkflowPropertySignaler.PropertyChanged(value, field, propertyName!);
        field = value;
}
