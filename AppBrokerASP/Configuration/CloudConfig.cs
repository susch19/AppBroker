using System.Security.Cryptography;
using System.Text;

namespace AppBrokerASP.Configuration;

public class CloudConfig
{
    public const string ConfigName = nameof(CloudConfig);
    public ushort CloudServerPort { get; set; }
    public string CloudServerHost { get; set; }
    public string ConnectionID { get; set; }
    public bool Enabled { get; set; }
    public string LocalHostName { get; set; }
    public bool UseSSL { get; set; }

    public CloudConfig()
    {
        CloudServerPort = 443;
        CloudServerHost = "smarthome.susch.eu";
        ConnectionID = "";
        LocalHostName = "localhost";
        UseSSL = true;
    }
}
