using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;
using CommunityToolkit.Mvvm.ComponentModel;

using SocketIOClient;

using System.Runtime.CompilerServices;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("lumi.weather", "WSDCGQ11LM")]
public partial class XiaomiTempSensor : ZigbeeDevice
{
    public event EventHandler<float>? TemperatureChanged;
    public new bool IsConnected => Available;

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
        //DashboardProperties.Add(new (nameof(temperature), 0, new TextStyle(FontSize: 24d, FontWeight: FontWeight.Bold)));
        //DashboardProperties.Add(new (nameof(humidity), 1, RowNr: 1));
        //DashboardProperties.Add(new (nameof(pressure), 2, RowNr: 1));
        //HistoryProperties.Add(new(nameof(temperature), "Temperatur", " °C", "XiaomiTempSensor", Colors.GetWithAlpha(0xFF, Colors.Accent.Yellow700), Colors.GetWithAlpha(0xFF, Colors.Accent.Yellow200)));
        //HistoryProperties.Add(new(nameof(humidity), "rel. Luftfeuchtigkeit", " %", "cloud", Colors.GetWithAlpha(0xFF, Colors.Accent.Blue700), Colors.GetWithAlpha(0xFF, Colors.Accent.Blue100)));
        //HistoryProperties.Add(new(nameof(pressure), "Luftdruck", " kPA","barometer", Colors.GetWithAlpha(0xFF, Colors.Accent.Green700), Colors.GetWithAlpha(0xFF, Colors.Accent.Green400)));
    }

    protected override void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "")
    {
        if (propertyName == nameof(Temperature))
            TemperatureChanged?.Invoke(this, (float)(object)value!);
        base.OnPropertyChanging(ref field, value, propertyName);
    }
}
