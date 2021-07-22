using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AppBokerASP.Devices.Heater
{
    public class HeaterConfig
    {

        [JsonProperty("dayOfWeek"), JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public DayOfWeek DayOfWeek { get; set; }
        [JsonProperty("timeOfDay")]
        public DateTime TimeOfDay { get; set; }
        [JsonProperty("temperature")]
        public double Temperature { get; set; }

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
            => new TimeTempMessageLE(hc.DayOfWeek, new TimeSpan(hc.TimeOfDay.Hour, hc.TimeOfDay.Minute, 0), (float)hc.Temperature);
        public static implicit operator HeaterConfig(TimeTempMessageLE ttm)
        {
            var dt = DateTime.Now;
            dt = dt.AddHours(ttm.Time.Hours - dt.Hour);
            dt = dt.AddMinutes(ttm.Time.Minutes - dt.Minute);
            return new HeaterConfig(ttm.DayOfWeek, dt, ttm.Temp);
        }
    }
}
