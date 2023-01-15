namespace AppBroker.PainlessMesh;

public class PainlessMeshSettings
{
    public const string ConfigName = nameof(PainlessMeshSettings);

    public bool Enabled { get; set; }
    public ushort ListenPort { get; set; }

    public string MQTTAddress { get; set; }
    public ushort MQTTPort { get; set; }
    public string MQTTClientId { get; set; }
    public string MQTTTopic { get; set; }
}
