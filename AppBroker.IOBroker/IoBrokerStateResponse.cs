using Newtonsoft.Json.Linq;

using System.Diagnostics;

namespace AppBroker.IOBroker;

[DebuggerDisplay("{_id}")]
public class IoBrokerStateResponse
{
    public IoBrokerStateResponse(JToken val, bool ack, long ts, int q, string from, long lc, string type, Common common, string id, Acl acl)
    {
        this.val = val;
        this.ack = ack;
        this.ts = ts;
        this.q = q;
        this.from = from;
        this.lc = lc;
        this.type = type;
        this.common = common;
        _id = id;
        this.acl = acl;
    }

    public JToken val { get; set; }
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
