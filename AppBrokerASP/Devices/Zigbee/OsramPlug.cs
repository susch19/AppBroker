using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public class OsramPlug : UpdateableZigbeeDevice
    {
        public bool State { get; set; }

        public OsramPlug(long nodeId, SocketIO socket) : base(nodeId, typeof(OsramPlug), socket)
        {
            ShowInApp = true;
        }

        public override async Task UpdateFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.On:
                    State = true;
                    await SetValue(nameof(State), State.ToString().ToLower());
                    break;
                case Command.Off:
                    State = false;
                    await SetValue(nameof(State), State.ToString().ToLower());
                    break;
                default:
                    break;
            }
        }
    }
}
