namespace AppBroker.IOBroker;

public class ZigbeeConfig
{
    public const string ConfigName = nameof(ZigbeeConfig);
    public string SocketIOUrl { get; set; }
    public string HttpUrl { get; set; }
    public string HistoryPath { get; set; }
    public bool NewSocketIoversion { get; set; }
    public bool? Enabled { get; set; } // Nullable so old configs have it enabled

    public ZigbeeConfig()
    {
        SocketIOUrl = "";
        HttpUrl = "";
        HistoryPath = "";
        NewSocketIoversion = false;
        Enabled = true;
    }
}
