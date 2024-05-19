using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("TRADFRI remote control", "E1524/E1810")]
public class TradfriRemoteControl : ZigbeeDevice
{
    public TradfriRemoteControl(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(TradfriRemoteControl))
    {
        ShowInApp = false;
    }
}
