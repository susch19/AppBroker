using CommunityToolkit.Mvvm.ComponentModel;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("E1525/E1745")]
public partial class TradfriMotionSensor : ZigbeeDevice
{
    [ObservableProperty]
    private byte battery;

    [ObservableProperty]
    private long noMotion;

    [ObservableProperty]
    private bool occupancy;

    public TradfriMotionSensor(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }
}
