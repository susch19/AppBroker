using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

using nj = Newtonsoft.Json;

namespace PainlessMesh
{
    public partial class JsonSmarthomeMessage : BaseSmarthomeMessage
    {
        [nj.JsonProperty("p")]
        public List<JToken> Parameters { get; set; }
        [JsonIgnore]
        public override uint NodeId { get; set; }

        [JsonProperty("id")]
        public long LongNodeId { get; set; }

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

}
