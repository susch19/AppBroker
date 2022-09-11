using Newtonsoft.Json;
using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("E1525/E1745")]
public partial class TradfriMotionSensor : ZigbeeDevice
{

    public TradfriMotionSensor(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(TradfriMotionSensor))
    {
        ShowInApp = true;
    }
}
