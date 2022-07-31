namespace AppBrokerASP.Cloud;

public class AppCloudConfiguration
{
    public string Host { get; set; }
    public ushort Port { get; set; }
    public string Key { get; set; }
    public string Id { get; set; }
    public AppCloudConfiguration(string host, ushort port, byte[] key, string id)
    {
        Host = host;
        Port = port;
        Key = Convert.ToBase64String(key);
        Id = id;
    }
}
