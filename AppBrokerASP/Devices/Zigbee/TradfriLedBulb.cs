
using AppBroker.Core;

using CommunityToolkit.Mvvm.ComponentModel;

using Newtonsoft.Json.Linq;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("TRADFRI bulb E27 CWS opal 600lm", "TRADFRI bulb E14 CWS opal 600lm", "LED1624G9")]
public partial class TradfriLedBulb : UpdateableZigbeeDevice
{
    private byte brightness;

    public byte Brightness
    {
        get => brightness;
        set => SetProperty(ref brightness, Math.Clamp(value, (byte)0, (byte)100));
    }

    [ObservableProperty]
    private string color = "#0000FF";

    [ObservableProperty]
    private bool state;

    public TradfriLedBulb(long nodeId, SocketIO socket) :
        base(nodeId, socket)
    {
        ShowInApp = true;
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Off:
                State = false;
                await SetValue(nameof(State), State);
                break;
            case Command.On:
                State = true;
                await SetValue(nameof(State), State);
                break;
            case Command.Brightness:
                Brightness = parameters[0].ToObject<byte>();
                await SetValue(nameof(Brightness), Brightness);
                break;
            case Command.Color:
                Color = parameters[0].ToString();
                await SetValue(nameof(Color), Color);
                break;
        }
    }
}
