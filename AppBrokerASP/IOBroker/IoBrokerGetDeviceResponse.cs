using System.Diagnostics;

namespace AppBrokerASP.IOBroker
{
    [DebuggerDisplay("{_id}")]
    public class IoBrokerGetDeviceResponse
    {
        public IoBrokerGetDeviceResponse(string type, Common common, Native native, string from, long ts, string id, Acl acl)
        {
            this.type = type;
            this.common = common;
            this.native = native;
            this.from = from;
            this.ts = ts;
            _id = id;
            this.acl = acl;
        }

        public string type { get; set; }
        public Common common { get; set; }
        public Native native { get; set; }
        public string from { get; set; }
        public long ts { get; set; }
        public string _id { get; set; }
        public Acl acl { get; set; }
    }
}
