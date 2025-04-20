using AppBroker.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using nj = Newtonsoft.Json;

namespace AppBroker.App;

//[NonSucking.Framework.Serialization.Nooson]
public partial class JsonSmarthomeMessage : BaseSmarthomeMessage
{
    [nj.JsonProperty("p")]
    public List<JToken> Parameters { get; set; }

    public JsonSmarthomeMessage(long nodeId, MessageType messageType, Command command, params JToken[] parameters)
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
