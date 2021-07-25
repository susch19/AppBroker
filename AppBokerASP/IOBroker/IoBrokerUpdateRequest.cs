using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.IOBroker
{


    public class ZigbeeIOBrokerProperty
    {
        public ZigbeeIOBrokerProperty(string id, string type, Common common, Native native, string from, long ts, Acl acl, Enums enums)
        {
            _id = id;
            this.type = type;
            this.common = common;
            this.native = native;
            this.from = from;
            this.ts = ts;
            this.acl = acl;
            this.enums = enums;
        }

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
        public Common(string name, string type, bool read, bool write, object def, string role, int min, int max, string unit, Custom custom, string icon)
        {
            this.name = name;
            this.type = type;
            this.read = read;
            this.write = write;
            this.def = def;
            this.role = role;
            this.min = min;
            this.max = max;
            this.unit = unit;
            this.custom = custom;
            this.icon = icon;
        }

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
        public Custom(History0 history0, Sql0 sql0)
        {
            this.history0 = history0;
            this.sql0 = sql0;
        }

        public History0 history0 { get; set; }
        public Sql0 sql0 { get; set; }
    }

    public class History0
    {
        public History0(bool enabled, bool changesOnly, string debounce, string maxLength, int retention, int changesRelogInterval, int changesMinDelta, string aliasId)
        {
            this.enabled = enabled;
            this.changesOnly = changesOnly;
            this.debounce = debounce;
            this.maxLength = maxLength;
            this.retention = retention;
            this.changesRelogInterval = changesRelogInterval;
            this.changesMinDelta = changesMinDelta;
            this.aliasId = aliasId;
        }

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
        public Sql0(bool enabled, bool changesOnly, string debounce, int retention, int changesRelogInterval, int changesMinDelta, string storageType, string aliasId)
        {
            this.enabled = enabled;
            this.changesOnly = changesOnly;
            this.debounce = debounce;
            this.retention = retention;
            this.changesRelogInterval = changesRelogInterval;
            this.changesMinDelta = changesMinDelta;
            this.storageType = storageType;
            this.aliasId = aliasId;
        }

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
        public Native(string id, string art)
        {
            this.id = id;
            this.art = art;
        }

        public string id { get; set; }
        public string art { get; set; }
    }

    public class Acl
    {
        public Acl(int @object, int state, string owner, string ownerGroup)
        {
            _object = @object;
            this.state = state;
            this.owner = owner;
            this.ownerGroup = ownerGroup;
        }

        public int _object { get; set; }
        public int state { get; set; }
        public string owner { get; set; }
        public string ownerGroup { get; set; }
    }

    public class Enums
    {
    }
}
