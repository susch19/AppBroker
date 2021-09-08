using Newtonsoft.Json.Linq;

using PainlessMesh;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class ZigbeeLamp : UpdateableZigbeeDevice
    {
        public byte Brightness { get; set; }
        public bool State { get; set; }
        public int ColorTemp { get; set; }
        [JsonPropertyName("transitionTime")]
        public float Transition_Time { get; set; }

        public ZigbeeLamp(long nodeId, Type t, SocketIO socket) : base(nodeId, t, socket)
        {
            ShowInApp = true;
        }

        public override async void OptionsFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.Delay:
                    Transition_Time = parameters[0].ToObject<float>();
                    await SetValue(nameof(Transition_Time), Transition_Time);
                    break;
            }
        }

        public override async void UpdateFromApp(Command command, List<JToken> parameters)
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
