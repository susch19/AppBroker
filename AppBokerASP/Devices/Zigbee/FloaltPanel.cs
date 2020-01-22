using Newtonsoft.Json.Linq;
using PainlessMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class FloaltPanel : ZigbeeDevice
    {
        public byte Brightness { get; set; }
        public bool State { get; set; }
        public int ColorTemp { get; set; }
        [JsonPropertyName("transitionTime")]
        public float Transition_Time { get; set; }

        private readonly string baseUpdateUrl;
        public FloaltPanel(ulong nodeId, string baseUpdateUrl) : base(nodeId, typeof(FloaltPanel))
        {
            ShowInApp = true;
            this.baseUpdateUrl = baseUpdateUrl;
        }

        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.Time:
                    Transition_Time = parameters[0].ToObject<float>();
                    UpdateZigbeeDeviceRequest(nameof(Transition_Time).ToLower(), Transition_Time);
                    break;
            }
        }

        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {
            switch (command)
            {
                case Command.Temp:
                    ColorTemp = parameters[0].ToObject<byte>();
                    UpdateZigbeeDeviceRequest(nameof(ColorTemp).ToLower(), ColorTemp);
                    break;
                case Command.Brightness:
                    Brightness = parameters[0].ToObject<byte>();
                    UpdateZigbeeDeviceRequest(nameof(Brightness).ToLower(), Brightness);
                    break;
                case Command.Off:
                    State = !State;
                    UpdateZigbeeDeviceRequest(nameof(State).ToLower(), State);
                    break;
                default:
                    break;
            }
        }

        private WebResponse UpdateZigbeeDeviceRequest<T>(string valuename, T value)
        {
            var request = WebRequest.CreateHttp(string.Format("{0}.{1}?value={2}", baseUpdateUrl, valuename, value));
            return request.GetResponse();
        }
    }
}
