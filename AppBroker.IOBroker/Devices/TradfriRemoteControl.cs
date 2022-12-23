using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("TRADFRI remote control", "E1524/E1810")]
public class TradfriRemoteControl : ZigbeeDevice
{
    public TradfriRemoteControl(long nodeId, SocketIO socket) : base(nodeId, socket, nameof(TradfriRemoteControl))
    {
        ShowInApp = false;
    }
}
