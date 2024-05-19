namespace AppBroker.IOBroker.Devices;

//[DeviceName("T2011")]
public class OsvallaPanel : ZigbeeLamp
{
    public OsvallaPanel(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(OsvallaPanel))
    {
    }
}
