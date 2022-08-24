using CommunityToolkit.Mvvm.ComponentModel;

using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("MCCGQ11LM")]

public partial class ZigbeeContactSensor : ZigbeeDevice
{
    [ObservableProperty]
    private double temperature;

    [ObservableProperty]
    private bool state;

    public ZigbeeContactSensor(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }
}
