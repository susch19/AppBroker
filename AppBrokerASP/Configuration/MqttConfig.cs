namespace AppBrokerASP.Configuration;

public class MqttConfig
{
    public const string ConfigName = nameof(MqttConfig);
    
    public bool Enabled { get; set; }
    public int ConnectionBacklog { get; set; }
    public int Port { get; set; }

    public MqttConfig()
    {
        Enabled = false;
        ConnectionBacklog = 10;
        Port = 8999;
    }
}
