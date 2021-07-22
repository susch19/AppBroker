using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PainlessMesh.Ota
{
    public enum Hardware
    {
        ESP8266,
        ESP32,
    }

    public class OtaAnnounceSmarthomeMessage : GeneralSmarthomeMessage
    {
        public string MD5Hash { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Hardware Hardware { get; set; }
        public string Role { get; set; }
        public bool Forced { get; set; }
        public int NoPart{ get; set; }
        public int From { get; set; }
        public int Dest { get; set; }
    }

    public class OtaDataSmarthomeMessage : OtaAnnounceSmarthomeMessage
    {

    }

}
