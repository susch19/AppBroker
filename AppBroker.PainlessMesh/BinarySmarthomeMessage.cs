﻿using AppBroker.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AppBroker.PainlessMesh;

[NonSucking.Framework.Serialization.Nooson]
public partial class BinarySmarthomeMessage : BaseSmarthomeMessage
{
    public SmarthomeHeader Header { get; set; }
    //public override uint NodeId { get => base.NodeId; set => base.NodeId = value; }
    [JsonProperty("m"), JsonConverter(typeof(StringEnumConverter))]
    public override MessageType MessageType { get => base.MessageType; set => base.MessageType = value; }
    [JsonConverter(typeof(StringEnumConverter)), JsonProperty("c")]
    public override Command Command { get => base.Command; set => base.Command = value; }


    public ByteLengthList Parameters { get; set; }
    //public List<byte> Parameters2 { get; set; }

    public BinarySmarthomeMessage(uint nodeId, MessageType messageType, Command command, params byte[][] parameters) : this(nodeId, messageType, command, new ByteLengthList(parameters))
    {
    }

    public BinarySmarthomeMessage(uint nodeId, MessageType messageType, Command command, ByteLengthList parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters;
        Header = new SmarthomeHeader(1, SmarthomePackageType.Normal);
    }

    public BinarySmarthomeMessage(SmarthomePackageType packageType, uint nodeId, MessageType messageType, Command command, params byte[][] parameters) : this(packageType, nodeId, messageType, command, new ByteLengthList(parameters))
    {
    }

    public BinarySmarthomeMessage(SmarthomePackageType packageType, uint nodeId, MessageType messageType, Command command, ByteLengthList parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters;
        Header = new SmarthomeHeader(1, packageType);
    }

    public BinarySmarthomeMessage()
    {

    }
}

public partial struct SmarthomeHeader
{
    public SmarthomeHeader(byte version, SmarthomePackageType type)
    {
        Version = version;
        Type = type;
    }

    public byte Version { get; set; }
    public SmarthomePackageType Type { get; set; }
}
