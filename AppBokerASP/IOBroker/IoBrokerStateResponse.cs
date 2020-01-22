using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{
    public class IoBrokerStateResponse
    {
        public Newtonsoft.Json.Linq.JToken val { get; set; }
        public bool ack { get; set; }
        public long ts { get; set; }
        public int q { get; set; }
        public string from { get; set; }
        public long lc { get; set; }
        public string type { get; set; }
        public Common common { get; set; }
        public string _id { get; set; }
        public Acl acl { get; set; }
    }
}
