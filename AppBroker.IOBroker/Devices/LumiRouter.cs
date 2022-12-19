using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("lumi.router")]
public class LumiRouter : ZigbeeDevice
{
    public LumiRouter(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(LumiRouter))
    {
        ShowInApp = false;
    }
}
