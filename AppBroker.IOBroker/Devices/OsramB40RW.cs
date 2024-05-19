using AppBroker.Core.Devices;

namespace AppBroker.IOBroker.Devices;

[DeviceName("Classic B40 TW - LIGHTIFY", "AB32840")]
public class OsramB40RW : ZigbeeLamp
{
    public OsramB40RW(long nodeId, SocketIOClient.SocketIO socket) : base(nodeId, socket, nameof(OsramB40RW))
    {
    }
}
