using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.Collections;
using System.Text;

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

}
