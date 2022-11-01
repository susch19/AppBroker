using AppBroker.Core;

using Jint.Native;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee;

public abstract partial class ZigbeeLamp : UpdateableZigbeeDevice
{


    public ZigbeeLamp(long nodeId, SocketIO socket, string typeName) : base(nodeId, socket, typeName)
    {
        ShowInApp = true;
    }

    public override async void OptionsFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Delay:
                var transitionTime = parameters[0].ToObject<float>();
                SetState(nameof(transitionTime), transitionTime);
                await SetValue(nameof(transitionTime), transitionTime);
                break;
        }
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Temp:
                var colorTemp = parameters[0].ToObject<int>();
                SetState(nameof(colorTemp), colorTemp);
                await SetValue(nameof(colorTemp), colorTemp);
                break;
            case Command.Brightness:
                var brightness = Math.Clamp(parameters[0].ToObject<byte>(), (byte)0, (byte)100);
                SetState(nameof(brightness), brightness);
                await SetValue(nameof(brightness), brightness);
                break;
            case Command.SingleColor:
                var state = true;
                SetState(nameof(state), state);
                await SetValue(nameof(state), state);
                break;
            case Command.Off:
                state = false;
                SetState(nameof(state), state);
                await SetValue(nameof(state), state);
                break;
            default:
                break;
        }
    }
}
