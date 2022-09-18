using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("lumi.router")]
public class LumiRouter : ZigbeeDevice
{
    public LumiRouter(long nodeId, SocketIO socket) : base(nodeId, socket,nameof(LumiRouter))
    {
        ShowInApp = false;
    }
}
