using AppBroker.Core.Devices;


namespace AppBroker.IOBroker.Devices;

[DeviceName("Plug 01", "AB3257001NJ")]
public class OsramPlug : ZigbeeSwitch
{
    public OsramPlug(long nodeId,  SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(OsramPlug))
    {
        ShowInApp = true;
    }
}
