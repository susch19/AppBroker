using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PainlessMesh;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class TradfriLedBulb : UpdateableZigbeeDevice
    {
        private byte brightness;

        [JsonProperty("available")]
        public new bool Available { get; set; }

        [JsonProperty("brightness")]
        public byte Brightness
        {
            get => brightness;
            set
            {
                brightness = Math.Clamp(value, (byte)0, (byte)100);
            }
        }

        [JsonProperty("color")]
        public string Color { get; set; } = "#0000FF";

        [JsonProperty("state")]
        public bool State { get; set; }

        public TradfriLedBulb(long nodeId, string baseUpdateUrl, SocketIO socket) :
            base(nodeId, typeof(TradfriLedBulb), socket)
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
}
