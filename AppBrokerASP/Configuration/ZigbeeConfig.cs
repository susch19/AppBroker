namespace AppBrokerASP.Configuration
{
    public class ZigbeeConfig
    {
        public const string ConfigName = nameof(ZigbeeConfig);
        public string SocketIOUrl { get; set; }
        public string HttpUrl { get; set; }
        public string HistoryPath { get; set; }
        public bool NewSocketIoversion { get; set; }

        public ZigbeeConfig()
        {
            SocketIOUrl = "";
            HttpUrl = "";
            HistoryPath = "";
            NewSocketIoversion = false;
        }
    }
}
