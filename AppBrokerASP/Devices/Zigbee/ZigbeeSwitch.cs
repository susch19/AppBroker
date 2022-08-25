using AppBroker.Core;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json.Linq;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

public abstract partial class ZigbeeSwitch : UpdateableZigbeeDevice
{
    [ObservableProperty]
    private bool state;

    protected ZigbeeSwitch(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.On:
                State = true;
                await SetValue(nameof(State), State);
                break;

            case Command.Off:
                State = false;
                await SetValue(nameof(State), State);
                break;
            case Command.None:
                Console.WriteLine(string.Join(",", parameters.Select(x => x.ToObject<string>())));
                break;
        }
    }
}
