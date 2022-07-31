using System.Security.Cryptography;
using System.Text;

namespace AppBrokerASP.Configuration;

public class CloudConfig
{
    public const string ConfigName = nameof(CloudConfig);
    public ushort CloudServerPort { get; set; }
    public string CloudServerHost { get; set; }
    public string ConnectionID { get; set; }
    public bool Enable { get; set; }

    public CloudConfig()
    {
        CloudServerPort = 443;
        CloudServerHost = "smarthome.susch.eu";
        ConnectionID = "";
    }
}
