﻿using AppBroker.Core;

using Newtonsoft.Json.Linq;

namespace AppBroker.IOBroker.Devices;

public abstract partial class ZigbeeSwitch : UpdateableZigbeeDevice
{

    protected ZigbeeSwitch(long nodeId, SocketIOClient.SocketIO socket, string typeName) : base(nodeId, socket, typeName)
    {
        ShowInApp = true;
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.On:
                SetState("state", true);
                await SetValue("state", true);
                break;

            case Command.Off:
                SetState("state", false);
                await SetValue("state", false);
                break;
            case Command.None:
                Console.WriteLine(string.Join(",", parameters.Select(x => x.ToObject<string>())));
                break;
        }
    }
}
