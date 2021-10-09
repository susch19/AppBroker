using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PainlessMesh;

using SocketIOClient;

using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public abstract class ZigbeeLamp : UpdateableZigbeeDevice
    {
        public byte Brightness { get; set; }
        public bool State { get; set; }
        public int ColorTemp { get; set; }
        [JsonProperty("transition_Time")]
        public float TransitionTime { get; set; }

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
}
