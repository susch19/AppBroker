using Elsa.Activities.StateMachine;

using SocketIOClient;

using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using AppBrokerASP.IOBroker;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("TS011F_plug_1")]
public partial class TuyaSwitchSensor : ZigbeeSwitch
{
    [ObservableProperty]
    private bool childLock;

    [ObservableProperty]
    private float current;

    [ObservableProperty]
    private float energy;

    [ObservableProperty]
    [property: JsonProperty("load_power")]
    private float loadPower;

    [ObservableProperty]
    private float voltage;

    public TuyaSwitchSensor(long nodeId, SocketIO socket) : base(nodeId, socket)
    {
        ShowInApp = true;
    }

    public override async Task<List<IoBrokerHistory>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        var load_power = GetHistory(start, end, "load_power");
        var current = GetHistory(start, end, "current");
        var energy = GetHistory(start, end, "energy");
        var voltage = GetHistory(start, end, "voltage");

        var result = new List<IoBrokerHistory>
            {
                await load_power,
                await current,
                await energy,
                await voltage,

            };
        return result;
    }
}
