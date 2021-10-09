namespace AppBrokerASP.Configuration
{
    public class ServerConfig
    {
        public const string ConfigName = nameof(ServerConfig);
        public ushort ListenPort { get; set; }
        public List<string> ListenUrls { get; set; }
        public string InstanceName {  get; set; }
        public string ClusterId {  get; set; }

        public ServerConfig()
        {
            ListenPort = 0;
            ListenUrls = new();
            InstanceName = "AppBroker";
            ClusterId = "";
        }
    }
}