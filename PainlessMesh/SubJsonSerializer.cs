using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PainlessMesh
{
    public class SubJsonSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<uint, Sub>));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => serializer.Deserialize<Sub[]>(reader).ToDictionary(x => x.NodeId, x => x);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => serializer.Serialize(writer, ((Dictionary<uint, Sub>)value).Values);

    }

}
