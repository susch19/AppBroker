using AppBroker.Core;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json.Linq;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

public abstract partial class ZigbeeLamp : UpdateableZigbeeDevice
{
    [ObservableProperty]
    private byte brightness;

    [ObservableProperty]
    private bool state;

    [ObservableProperty]
    private int colorTemp;

    [ObservableProperty]
    [property: JsonProperty("transition_Time")]
    private float transitionTime;

    public ZigbeeLamp(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }

    public override async void OptionsFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Delay:
                TransitionTime = parameters[0].ToObject<float>();
                await SetValue(nameof(TransitionTime), TransitionTime);
                break;
        }
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Temp:
                ColorTemp = parameters[0].ToObject<int>();
                await SetValue(nameof(ColorTemp), ColorTemp);
                break;
            case Command.Brightness:
                Brightness = parameters[0].ToObject<byte>();
                await SetValue(nameof(Brightness), Brightness);
                break;
            case Command.SingleColor:
                State = true;
                await SetValue(nameof(State), State);
                break;
            case Command.Off:
                State = false;
                await SetValue(nameof(State), State);
                break;
            default:
                break;
        }
    }
}
