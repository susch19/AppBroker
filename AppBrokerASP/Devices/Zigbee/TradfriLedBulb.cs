
using AppBroker.Core;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("TRADFRI bulb E27 CWS opal 600lm", "TRADFRI bulb E14 CWS opal 600lm", "LED1624G9")]
[AppBroker.ClassPropertyChangedAppbroker]
public partial class TradfriLedBulb : UpdateableZigbeeDevice
{
    [AppBroker.IgnoreField]
    private byte brightness;

    public byte Brightness
    {
        get => brightness;
        set => _ = RaiseAndSetIfChanged(ref brightness, Math.Clamp(value, (byte)0, (byte)100));
    }

    private string color = "#0000FF";
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
