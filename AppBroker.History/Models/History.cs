using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBroker.History.Models;

public class History
{
    public static History Empty => new History();

    public History(List<HistoryRecord> historyRecords, string propertyName)
    {
        HistoryRecords = historyRecords;
        PropertyName = propertyName;
    }
    public History()
    {
        HistoryRecords = [];
        PropertyName = "";
    }

    public List<HistoryRecord> HistoryRecords { get; set; }

    public string PropertyName { get; set; }

    public History(string propertyName) : this()
    {
        PropertyName = propertyName;
    }
}

public record HistoryRecord
{
    [JsonConverter(typeof(DoubleEverythingConverter))]
    public double? Val { get; init; }
    public string? StringVal { get; init; }
    public long Ts { get; init; }

    public HistoryRecord()
    {

    }
    public HistoryRecord(double? value, long timestamp)
    {
        Val = value;
        Ts = timestamp;
    }
    public HistoryRecord(string? value, long timestamp)
    {
        StringVal = value;
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
