using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Linq;

using nj = Newtonsoft.Json;

namespace PainlessMesh
{
    public partial class JsonSmarthomeMessage : BaseSmarthomeMessage
    {
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

}
