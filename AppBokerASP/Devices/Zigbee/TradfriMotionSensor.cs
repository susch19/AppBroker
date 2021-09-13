using Newtonsoft.Json;
using SocketIOClient;

namespace AppBokerASP.Devices.Zigbee
{
    [DeviceName("E1525/E1745")]
    public class TradfriMotionSensor : ZigbeeDevice
    {
        [JsonProperty("available")]
        public new bool Available { get; set; }

        [JsonProperty("battery")]
        public byte Battery { get; set; }

        [JsonProperty("no_motion")]
        public long NoMotion { get; set; }

        [JsonProperty("occupancy")]
        public bool Occupancy { get; set; }

        public TradfriMotionSensor(long nodeId, SocketIO socket) : base(nodeId, socket)
        {
            ShowInApp = true;
        }
    }
}
