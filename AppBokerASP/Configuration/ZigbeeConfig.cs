using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Configuration
{
    public class ZigbeeConfig
    {
        public const string ConfigName = nameof(ZigbeeConfig);
        public string SocketIOUrl { get; set; }
        public string HttpUrl { get; set; }
        public string HistoryPath { get; set; }

        public ZigbeeConfig()
        {
            SocketIOUrl = "";
            HttpUrl = "";
            HistoryPath = "";
        }
    }
}
