using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public class OsramPlug : UpdateableZigbeeDevice
    {
        public bool State { get; set; }

        public OsramPlug(long nodeId, string baseUpdateUrl, SocketIO socket) : base(nodeId, baseUpdateUrl, typeof(OsramPlug), socket)
        {
            ShowInApp = true;
        }

        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.On:
                    State = true;
                    _ = UpdateZigbeeDeviceRequest(nameof(State).ToLower(), State.ToString().ToLower());
                    break;
                case Command.Off:
                    State = false;
                    _ = UpdateZigbeeDeviceRequest(nameof(State).ToLower(), State.ToString().ToLower());
                    break;
                default:
                    break;
            }
        }
    }
}
