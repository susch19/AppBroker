using AppBrokerASP.Devices.Zigbee;
using Newtonsoft.Json.Linq;
using PainlessMesh;
using SocketIOClient;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public abstract class ZigbeeSwitch : UpdateableZigbeeDevice
    {
        public bool State { get; set; }

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
}
