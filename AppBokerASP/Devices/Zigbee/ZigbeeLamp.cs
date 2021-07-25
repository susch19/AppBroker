using Newtonsoft.Json.Linq;
using PainlessMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class ZigbeeLamp : UpdateableZigbeeDevice
    {
        public byte Brightness { get; set; }
        public bool State { get; set; }
        public int ColorTemp { get; set; }
        [JsonPropertyName("transitionTime")]
        public float Transition_Time { get; set; }

        public ZigbeeLamp(long nodeId, string baseUpdateUrl, Type t) : base(nodeId, baseUpdateUrl, t)
        {
            ShowInApp = true;
        }

        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.Delay:
                    Transition_Time = parameters[0].ToObject<float>();
                    _ = UpdateZigbeeDeviceRequest(nameof(Transition_Time).ToLower(), Transition_Time);
                    break;
            }
        }

        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.Temp:
                    ColorTemp = parameters[0].ToObject<int>();
                    _ = UpdateZigbeeDeviceRequest(nameof(ColorTemp).ToLower(), ColorTemp);
                    break;
                case Command.Brightness:
                    Brightness = parameters[0].ToObject<byte>();
                    _ = UpdateZigbeeDeviceRequest(nameof(Brightness).ToLower(), Brightness);
                    break;
                case Command.Off:
                    State = !State;
                    _ = UpdateZigbeeDeviceRequest(nameof(State).ToLower(), State.ToString().ToLower());
                    break;
                default:
                    break;
            }
        }

    }
}
