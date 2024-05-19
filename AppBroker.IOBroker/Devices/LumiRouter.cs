using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("lumi.router")]
public class LumiRouter : ZigbeeDevice
{
    public LumiRouter(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(LumiRouter))
    {
        ShowInApp = false;
    }
}
