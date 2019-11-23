using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using nj = Newtonsoft.Json;

namespace PainlessMesh
{

    public class BasicMessage
    {
        public uint dest { get; set; }
        public uint from { get; set; }
        public PackageType type { get; set; }
    }


    public class TimeSync : BasicMessage
    {
        public TimeSyncMsg msg { get; set; }
    }

    public class TimeSyncMsg
    {
        public int type { get; set; }
        public uint t0 { get; set; }
        public uint t1 { get; set; }
        public uint t2 { get; set; }
    }


    public class SingleAdressedMessage<T> : BasicMessage
    {
        public T msg { get; set; }
    }

    public class BroadcastMessage<T> : SingleAdressedMessage<T> { }

    public class HelperMessage
    {
        public int type { get; set; }
    }
    public class GeneralSmarthomeMessage
    {
        [JsonPropertyName("id"), nj.JsonProperty("id")]
        public uint NodeId { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter)), JsonPropertyName("m"), nj.JsonProperty("m")]
        public MessageType MessageType { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter)), JsonPropertyName("c"), nj.JsonProperty("c")]
        public Command Command { get; set; }
        [JsonPropertyName("p"), nj.JsonProperty("p"), nj.JsonConverter(typeof(SingleOrListConverter<JsonElement>))]
        public List<JsonElement> Parameters { get; set; }
        public GeneralSmarthomeMessage(uint nodeId, MessageType messageType, Command command, params JsonElement[] parameters)
        {
            NodeId = nodeId;
            MessageType = messageType;
            Command = command;
            Parameters = parameters.ToList();
        }
        public GeneralSmarthomeMessage()
        {
        }

    }
    public class SingleOrListConverter<T> : nj.JsonConverter
    {
        public override bool CanConvert(Type objectType) => (objectType == typeof(List<T>));

        public override object ReadJson(nj.JsonReader reader, Type objectType, object existingValue, nj.JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
                return JsonSerializer.Deserialize<List<T>>(token.ToString());
            return new List<T> { token.ToObject<T>() };
        }

        public override void WriteJson(nj.JsonWriter writer, object value, nj.JsonSerializer serializer)
        {
            var list = (List<T>)value;
            if (list.Count == 1)
                value = list[0];
            writer.WriteValue(JsonSerializer.Serialize(value));
            //serializer.Serialize(writer, value);
        }

        public override bool CanWrite => true;
    }

    //public class JsonElementList : IList<JsonElement>
    //{
    //    private IList<string> stringList { get;  }

    //    public JsonElementList(IList<string> elements)
    //    {
    //        stringList = elements;
    //    }

    //    public JsonElement this[int index] { get => JsonSerializer.Deserialize<JsonElement>(stringList[index]); set => stringList[index] = value.ToString(); }

    //    public int Count => stringList.Count;

    //    public bool IsReadOnly => true;

    //    public void Add(JsonElement item) => stringList.Add(item.ToString());
    //    public void Clear() => stringList.Clear();
    //    public bool Contains(JsonElement item) => stringList.Contains(item.ToString());
    //    public void CopyTo(JsonElement[] array, int arrayIndex) => throw new NotSupportedException();
    //    public IEnumerator<JsonElement> GetEnumerator() => stringList.Select(x=>JsonSerializer.Deserialize<JsonElement>(x)).GetEnumerator();
    //    public int IndexOf(JsonElement item) => stringList.IndexOf(item.ToString());
    //    public void Insert(int index, JsonElement item) => stringList.Insert(index, item.ToString());
    //    public bool Remove(JsonElement item) => stringList.Remove(item.ToString());
    //    public void RemoveAt(int index) => stringList.RemoveAt(index);
    //    IEnumerator IEnumerable.GetEnumerator() => stringList.Select(x => JsonSerializer.Deserialize<JsonElement>(x)).GetEnumerator();
    //}

    public class NodeSyncMessage : BasicMessage
    {

        [System.Text.Json.Serialization.JsonConverter(typeof(SubJsonSerializer))]
        public Dictionary<uint, Sub> subs { get; set; }
    }

    public class SubJsonSerializer : System.Text.Json.Serialization.JsonConverter<Dictionary<uint, Sub>>
    {

        public override Dictionary<uint, Sub> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var res = new Dictionary<uint, Sub>();

            res = System.Text.Json.JsonSerializer.Deserialize<Sub[]>(ref reader).ToDictionary(x => x.NodeId, x => x);

            return res;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<uint, Sub> value, JsonSerializerOptions options)
        {
            System.Text.Json.JsonSerializer.Serialize<Sub[]>(writer, value.Values.ToArray());
        }
    }



    public class Sub
    {
        [JsonPropertyName("nodeId")]
        public uint NodeId { get; set; }
        public bool root { get; set; }

        [System.Text.Json.Serialization.JsonConverter(typeof(SubJsonSerializer))]
        [JsonPropertyName("subs")]
        public Dictionary<uint, Sub> Subs { get; set; }

        public DateTime LastReceived { get; internal set; }
        //public static explicit operator SubConnection(Sub s)
        //    => new SubConnection() { NodeId = s.nodeId, Connections = s.subs.ToDictionary(x => x.nodeId, x => (SubConnection)x) };

        public bool ContainsId(uint id)
        {
            if (NodeId == id)
                return true;
            else if (Subs.Count == 0)
                return false;
            else
                return Subs.Any(x => x.Value.ContainsId(id));
        }

        public bool RemoveConnection(Sub connection)
        {
            if (connection == null || Subs.Remove(connection.NodeId))
                return true;

            return Subs.Any(x => x.Value.RemoveConnection(connection));
        }
    }

}
