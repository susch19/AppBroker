using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Globalization;

namespace AppBokerASP.IOBroker
{

    public class IoBrokerZigbee : IoBrokerObject
    {
        public long Id { get; set; }

        public IoBrokerZigbee(long id, BrokerEvent eventName, string adapterName, byte adapterInstance, string valueName, Parameter valueParameter)
            : base(eventName, adapterName, adapterInstance, valueName, valueParameter)
        {
            Id = id;
        }

        public static bool TryParse(string eventName, string s, out IoBrokerZigbee? obj)
        {
            obj = null;

            if (!Enum.TryParse<BrokerEvent>(eventName, true, out var en))
                return false;

            var array = JArray.Parse(s);
            var split = array[0].ToString().Split(".");
            var valueParameter = array[1].ToObject<Parameter>();

            if (split.Length < 4)
                return false;

            if (!byte.TryParse(split[1], out var instance))
                return false;

            if (!long.TryParse(split[2], NumberStyles.HexNumber, null, out var id))
                return false;

            obj = new IoBrokerZigbee(id, en, split[0], instance, split[3], valueParameter!);

            return true;
        }

        public override string ToString() => $"{Id}, {EventName}, {AdapterName}.{AdapterInstance}, {ValueName}:{ValueParameter.Value}, {ValueParameter.Quality}";
    }

    public class IoBrokerObject
    {
        public IoBrokerObject(BrokerEvent eventName, string adapterName, byte adapterInstance, string valueName, Parameter valueParameter)
        {
            EventName = eventName;
            AdapterName = adapterName;
            AdapterInstance = adapterInstance;
            ValueName = valueName;
            ValueParameter = valueParameter;
        }

        public BrokerEvent EventName { get; set; }
        public string AdapterName { get; set; }
        public byte AdapterInstance { get; set; }
        public string ValueName { get; set; }
        public Parameter ValueParameter { get; set; }
    }



    public class Parameter
    {
        public Parameter(JToken value)
        {
            Value = value;
            From = "";
        }

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
