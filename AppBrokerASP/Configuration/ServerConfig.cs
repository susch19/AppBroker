using System.Security.Cryptography;
using System.Text;

namespace AppBrokerASP.Configuration;

public class ServerConfig
{
    public const string ConfigName = nameof(ServerConfig);
    public ushort ListenPort { get; set; }
    public List<string> ListenUrls { get; set; }
    public string InstanceName { get; set; }
    public string ClusterId { get; set; }
    public string EncryptionPassword { get; set; }
    public bool EnableJavaScript { get; set; }

    public ServerConfig()
    {
        ListenPort = 0;
        ListenUrls = new();
        InstanceName = "AppBroker";
        ClusterId = "";
        EncryptionPassword = Encoding.UTF8.GetString(SHA256.Create().ComputeHash(
            Encoding.UTF8.GetBytes(
            InstanceName +
            Environment.CurrentDirectory +
            Environment.UserDomainName)));
        EnableJavaScript = true;
    }
}
