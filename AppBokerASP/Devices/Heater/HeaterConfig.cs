using nj =Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace AppBokerASP.Devices.Heater
{
    public class HeaterConfig
    {

        [JsonPropertyName("dayOfWeek"), JsonConverter(typeof(JsonStringEnumConverter))]
        public DayOfWeek DayOfWeek { get; set; }
        [JsonPropertyName("timeOfDay")]
        public DateTime TimeOfDay { get; set; }
        [JsonPropertyName("temperature")]
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

 
    }
}
