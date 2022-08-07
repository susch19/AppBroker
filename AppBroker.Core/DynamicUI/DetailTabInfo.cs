namespace AppBroker.Core.DynamicUI;

public class DetailTabInfo
{
    public int Id { get; set; }
    public string IconName { get; set; } = "";
    public int Order { get; set; }
    public LinkedDeviceTab? LinkedDevice { get; set; } // Have another device in a tab, like Heater and Temperature Sensor
}

