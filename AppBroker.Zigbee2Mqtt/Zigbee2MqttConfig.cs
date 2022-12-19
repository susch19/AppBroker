namespace AppBroker.Zigbee2Mqtt;

public class Zigbee2MqttConfig
{
    public const string ConfigName = nameof(Zigbee2MqttConfig);
    public bool Enabled { get; set; }
    public string Topic { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }

    public Zigbee2MqttConfig()
    {
        Enabled = false;
        Topic = "zigbee2mqtt";
        Address = "127.0.0.1";
        Port = 8999;
    }
}
