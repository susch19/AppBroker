using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{
    public class IoBrokerGetDeviceResponse
    {
        public string type { get; set; }
        public Common common { get; set; }
        public Native native { get; set; }
        public string from { get; set; }
        public long ts { get; set; }
        public string _id { get; set; }
        public Acl acl { get; set; }
    }
}
