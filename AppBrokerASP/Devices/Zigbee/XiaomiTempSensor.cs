using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;
using CommunityToolkit.Mvvm.ComponentModel;

using Elsa.Activities.StateMachine;

using Newtonsoft.Json.Linq;

using SocketIOClient;

using System.Runtime.CompilerServices;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("lumi.weather", "WSDCGQ11LM")]
public partial class XiaomiTempSensor : ZigbeeDevice
{
    public event EventHandler<float>? TemperatureChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    bool? overridenState;

    public new bool IsConnected => overridenState ?? Available;

    [ObservableProperty]
    private float humidity;

    [ObservableProperty]
    private float pressure;

    [ObservableProperty]
    private byte battery;

    [ObservableProperty]
    private float voltage;

    [ObservableProperty]
    private float temperature;

    public XiaomiTempSensor(long id, SocketIO socket) : base(id, socket)
    {
        ShowInApp = true;
        Available = true;
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.None:
                var par = parameters.FirstOrDefault();
                if (par is null)
                    return;
                if (par.Type == JTokenType.Boolean)
                {
                    overridenState = par.ToObject<bool>();
                    SendDataToAllSubscribers();
                }
                break;
        }
    }
    protected override void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "")
    {
        if (propertyName == nameof(Temperature))
            TemperatureChanged?.Invoke(this, (float)(object)value!);
        base.OnPropertyChanging(ref field, value, propertyName);
    }
}
