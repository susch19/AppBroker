using AppBrokerASP.Cloud;
using AppBrokerASP.Configuration;

using Microsoft.AspNetCore.Mvc;

using System.Security.Cryptography;
using System.Text;

namespace AppBrokerASP.Controllers;
[Route("security")]
public class SecurityController : ControllerBase
{
    private readonly CloudConfig config;
    private readonly ServerConfig serverConfig;

    public SecurityController(CloudConfig config, ServerConfig serverConfig)
    {
        this.config = config;
        this.serverConfig = serverConfig;
    }

    [HttpGet]
    public AppCloudConfiguration GetSecurityInfo()
    {
        return new AppCloudConfiguration(config.CloudServerHost, config.CloudServerPort,
            SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(serverConfig.EncryptionPassword)), config.ConnectionID);
    }
}
