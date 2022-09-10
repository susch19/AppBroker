using AppBroker.Core;

using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee;

public abstract partial class ZigbeeSwitch : UpdateableZigbeeDevice
{
    private bool State { get; set; }

    protected ZigbeeSwitch(long nodeId, SocketIO socket, string typeName) : base(nodeId, socket, typeName)
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
