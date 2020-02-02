using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{

    public class IoBrokerZigbee : IoBrokerObject
    {
        public long Id { get; set; }

        public static bool TryParse(string s, out IoBrokerZigbee obj)
        {
            obj = new IoBrokerZigbee();
            var i = s[2..].IndexOf('"')+2;
            if (!Enum.TryParse<BrokerEvent>(s[2..i], true, out var en))
                return false;
            obj.EventName = en;

            i = s[++i..].IndexOf('"') + 1+i;
            var split = s[i..(s[i..].IndexOf('"')+i)].Split('.');

            int splitIndex = -1;
            byte instanceNum;
            while (!byte.TryParse(split[++splitIndex], out instanceNum) && splitIndex+1 < split.Length)
            { }
            if (splitIndex+1 == split.Length)
                return false;
            obj.AdapterInstance = instanceNum;
            obj.AdapterName = string.Join('.', split[..(splitIndex)]);

            if (!long.TryParse(split[splitIndex + 1], System.Globalization.NumberStyles.HexNumber, null, out var id))
                return false;
            obj.Id = id;
            obj.ValueName = split.Last();

            obj.ValueParameter = s[(s[i..].IndexOf(',')+i+1)..^1].ToDeObject<Parameter>();
            
            return true;
        }

        public override string ToString() => $"{Id}, {EventName}, {AdapterName}.{AdapterInstance}, {ValueName}:{ValueParameter.Value}, {ValueParameter.Quality}";
    }

    public class IoBrokerObject
    {
        public BrokerEvent EventName { get; set; }
        public string AdapterName { get; set; }
        public byte AdapterInstance { get; set; }
        public string ValueName { get; set; }
        public Parameter ValueParameter { get; set; }

        public static bool TryParse(string s, out IoBrokerObject obj)
        {
            obj = new IoBrokerObject();
            var i = s[2..].IndexOf('"');
            if (!Enum.TryParse<BrokerEvent>(s[2..i], out var en))
                return false;
            obj.EventName = en;

            i = s[++i..].IndexOf('"') + 1;
            var split = s[i..s[i..].IndexOf('"')].Split('.');

            int splitIndex = 0;
            byte instanceNum;
            while (!byte.TryParse(split[splitIndex++], out instanceNum) || splitIndex < split.Length)
            { }
            if (splitIndex < split.Length)
                return false;
            obj.AdapterInstance = instanceNum;
            obj.AdapterName = string.Join('.', split[..(splitIndex - 1)]);

            obj.ValueName = split.Last();

            obj.ValueParameter = s[s[i..].IndexOf(',')..].ToDeObject<Parameter>();

            return true;
        }
    }



    public class Parameter
    {
        [JsonProperty("val")]
        public JToken Value { get; set; }
        [JsonProperty("ack")]
        public bool Acknowledged { get; set; }
        [JsonProperty("ts")]
        public long TimeStamp { get; set; }
        [JsonProperty("q")]
        public Quality Quality { get; set; }
        [JsonProperty("from")]
        public string From { get; set; }
        [JsonProperty("lc")]
        public long lc { get; set; }
    }

    public enum Quality
    {
        Good = 0x00,
        GeneralBadProblem = 0x01,
        NoConnection = 0x02,

        SubstituteValueFromController = 0x10,
        SubstituteValueFromDeviceOrInstance = 0x40,
        SubstituteValueFromSensor = 0x80,

        GeneralProblemInstance = 0x11,
        GeneralProblemDevice = 0x41,
        GeneralProblemSensor = 0x81,

        InstanceNotConnected = 0x12,
        DeviceNotConnected = 0x42,
        SensorNotConnected = 0x82,

        DeviceReportsError = 0x44,
        SensorReportsError = 0x84
    }

    public enum BrokerEvent
    {
        StateChange,
        ObjectChange
    }

}
