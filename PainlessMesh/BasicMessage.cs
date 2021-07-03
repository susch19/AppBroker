using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nj = Newtonsoft.Json;
using Azura;

namespace PainlessMesh
{

    public abstract class BaseSmarthomeMessage
    {
        [JsonProperty("id")]
        public virtual uint NodeId { get; set; }

        [JsonProperty("m"), JsonConverter(typeof(StringEnumConverter))]
        public virtual MessageType MessageType { get; set; }

        [JsonProperty("c"), JsonConverter(typeof(StringEnumConverter))]
        public virtual Command Command { get; set; }

    }

    [Azura]
    public partial class BinarySmarthomeMessage : BaseSmarthomeMessage
    {
        [Azura]
        public bool Header { get; set; }
        [Azura]
        public override uint NodeId { get => base.NodeId; set => base.NodeId = value; }
        [Azura]
        public override MessageType MessageType { get => base.MessageType; set => base.MessageType = value; }
        [Azura]
        public override Command Command { get => base.Command; set => base.Command = value; }

        [Azura]
        [nj.JsonProperty("p")/*, nj.JsonConverter(typeof(SingleOrListConverter<JsonElement>))*/]
        public ByteLengthList Parameters { get; set; }

        public BinarySmarthomeMessage(uint nodeId, MessageType messageType, Command command, params byte[][] parameters)
        {
            NodeId = nodeId;
            MessageType = messageType;
            Command = command;
            Parameters = new ByteLengthList(parameters);
        }
        public BinarySmarthomeMessage()
        {

        }
    }

    [Azura]
    public partial class JsonSmarthomeMessage : BaseSmarthomeMessage
    {
        [Azura]
        [nj.JsonProperty("p")]
        public List<JToken> Parameters { get; set; }
        public JsonSmarthomeMessage(uint nodeId, MessageType messageType, Command command, params JToken[] parameters)
        {
            NodeId = nodeId;
            MessageType = messageType;
            Command = command;
            Parameters = parameters.ToList();
        }

        public JsonSmarthomeMessage()
        {

        }
    }


    public class SubJsonSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<uint, Sub>));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var res = new Dictionary<uint, Sub>();

            res = serializer.Deserialize<Sub[]>(reader).ToDictionary(x => x.NodeId, x => x);

            return res;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var subs = value as Dictionary<long, Sub>;
            serializer.Serialize(writer, subs.Values);
        }
    }



    public class Sub : IEquatable<Sub>
    {
        [JsonProperty("nodeId")]
        public uint NodeId { get; set; }

        [JsonConverter(typeof(SubJsonSerializer))]
        [JsonProperty("subs")]
        public Dictionary<long, Sub> Subs { get; set; }

        public override bool Equals(object obj) => Equals(obj as Sub);
        public bool Equals(Sub other) => other != null && NodeId == other.NodeId;

        public static bool operator ==(Sub left, Sub right) => left?.Equals(right) ?? right == null;
        public static bool operator !=(Sub left, Sub right) => !(left == right);
    }

}
