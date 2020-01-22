using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{
    public class IoBrokerUpdateRequest
    {
        public ZigbeeIOBrokerProperty[] properties { get; set; }
    }

    public class ZigbeeIOBrokerProperty
    {
        public string _id { get; set; }
        public string type { get; set; }
        public Common common { get; set; }
        public Native native { get; set; }
        public string from { get; set; }
        public long ts { get; set; }
        public Acl acl { get; set; }
        public Enums enums { get; set; }
    }

    public class Common
    {
        public string name { get; set; }
        public string type { get; set; }
        public bool read { get; set; }
        public bool write { get; set; }
        public object def { get; set; }
        public string role { get; set; }
        public int min { get; set; }
        public int max { get; set; }
        public string unit { get; set; }
        public Custom custom { get; set; }
        public string icon { get; set; }
    }

    public class Custom
    {
        public History0 history0 { get; set; }
        public Sql0 sql0 { get; set; }
    }

    public class History0
    {
        public bool enabled { get; set; }
        public bool changesOnly { get; set; }
        public string debounce { get; set; }
        public string maxLength { get; set; }
        public int retention { get; set; }
        public int changesRelogInterval { get; set; }
        public int changesMinDelta { get; set; }
        public string aliasId { get; set; }
    }

    public class Sql0
    {
        public bool enabled { get; set; }
        public bool changesOnly { get; set; }
        public string debounce { get; set; }
        public int retention { get; set; }
        public int changesRelogInterval { get; set; }
        public int changesMinDelta { get; set; }
        public string storageType { get; set; }
        public string aliasId { get; set; }
    }


    public class Native
    {
        public string id { get; set; }
        public string art { get; set; }
    }

    public class Acl
    {
        public int _object { get; set; }
        public int state { get; set; }
        public string owner { get; set; }
        public string ownerGroup { get; set; }
    }

    public class Enums
    {
    }
}
