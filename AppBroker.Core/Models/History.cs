using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBroker.Core.Models;

public class History
{
    public History(HistoryRecord[] historyRecords, string propertyName)
    {
        HistoryRecords = historyRecords;
        PropertyName = propertyName;
    }
    public History()
    {
        HistoryRecords = Array.Empty<HistoryRecord>();
        PropertyName = "";
    }

    public HistoryRecord[] HistoryRecords { get; set; }

    public string PropertyName { get; set; }

    public History(string propertyName) : this()
    {
        PropertyName = propertyName;
    }
}

public class HistoryRecord
{
    [JsonConverter(typeof(DoubleEverythingConverter))]
    public double? Val { get; set; }
    public long Ts { get; set; }

    public HistoryRecord()
    {

    }
    public HistoryRecord(double? value, long timestamp)
    {
        Val = value;
        Ts = timestamp;
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
