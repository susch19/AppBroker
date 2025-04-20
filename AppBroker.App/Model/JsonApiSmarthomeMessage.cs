using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


namespace AppBroker.Core;

public partial class JsonApiSmarthomeMessage : BaseSmarthomeMessage
{
    public List<JToken> Parameters { get; set; }


    public JsonApiSmarthomeMessage(uint nodeId, MessageType messageType, Command command, params JToken[] parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters.ToList();
    }

    public JsonApiSmarthomeMessage()
    {

    }
}
