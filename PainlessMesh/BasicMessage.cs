using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nj = Newtonsoft.Json;

namespace PainlessMesh
{
    public class GeneralSmarthomeMessage
    {
        [nj.JsonProperty("id")]
        public long NodeId { get; set; }
        [nj.JsonProperty("m"), JsonConverter(typeof(StringEnumConverter))]
        public MessageType MessageType { get; set; }
        [nj.JsonProperty("c"), JsonConverter(typeof(StringEnumConverter))]
        public Command Command { get; set; }
        [ nj.JsonProperty("p")/*, nj.JsonConverter(typeof(SingleOrListConverter<JsonElement>))*/]
        public List<JToken> Parameters { get; set; }
        public GeneralSmarthomeMessage(long nodeId, MessageType messageType, Command command, params JToken[] parameters)
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


    public class SubJsonSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<uint, Sub>));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var res = new Dictionary<long, Sub>();

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
        public long NodeId { get; set; }

        [JsonConverter(typeof(SubJsonSerializer))]
        [JsonProperty("subs")]
        public Dictionary<long, Sub> Subs { get; set; }

        public override bool Equals(object obj) => Equals(obj as Sub);
        public bool Equals(Sub other) => other != null && NodeId == other.NodeId;

        public static bool operator ==(Sub left, Sub right) => left?.Equals(right) ?? right == null;
        public static bool operator !=(Sub left, Sub right) => !(left == right);
    }

}
