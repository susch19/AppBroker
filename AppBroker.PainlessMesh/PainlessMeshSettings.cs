namespace AppBroker.PainlessMesh;

public class PainlessMeshSettings
{
    public const string ConfigName = nameof(PainlessMeshSettings);

    public bool Enabled { get; set; }
    public ushort ListenPort { get; set; }
}
