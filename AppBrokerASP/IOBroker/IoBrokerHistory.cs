

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBrokerASP.IOBroker;

public class IoBrokerHistory
{
    public IoBrokerHistory(HistoryRecord[] historyRecords, string propertyName)
    {
        HistoryRecords = historyRecords;
        PropertyName = propertyName;
    }
    public IoBrokerHistory()
    {
        HistoryRecords = Array.Empty<HistoryRecord>();
        PropertyName = "";
    }

    public HistoryRecord[] HistoryRecords { get; set; }

    public string PropertyName { get; set; }

    public IoBrokerHistory(string propertyName) : this()
    {
        PropertyName = propertyName;
    }

    public class HistoryRecord
    {
        [JsonConverter(typeof(DoubleEverythingConverter))]
        public double? val { get; set; }
        public long ts { get; set; }

        public HistoryRecord()
        {

        }

        private class DoubleEverythingConverter : JsonConverter<double?>
        {
            public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.Number => reader.GetDouble(),
                    JsonTokenType.True => 1d,
                    JsonTokenType.False => 0d,
                    _ => null,
                };
            }

            public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}
