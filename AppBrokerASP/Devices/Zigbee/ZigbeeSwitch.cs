using AppBroker.Core;

using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee;

[AppBroker.ClassPropertyChangedAppbroker]
public abstract partial class ZigbeeSwitch : UpdateableZigbeeDevice
{
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
        }
    }
}
