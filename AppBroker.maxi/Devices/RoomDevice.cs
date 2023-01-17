using AppBroker.Core.Devices;

namespace AppBroker.maxi.Devices;

internal class RoomDevice : Device
{
    public RoomDevice(long nodeId, string roomName) : base(nodeId, "room_main_device")
    {
        FriendlyName = $"{roomName}_virtual_room";
        ShowInApp = true;
    }
}
