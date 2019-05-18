using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace PainlessMesh
{

    public class BasicMessage
    {
        public uint dest { get; set; }
        public uint from { get; set; }
        public PackageType type { get; set; }
    }


    public class TimeSync : BasicMessage
    {
        public TimeSyncMsg msg { get; set; }
    }

    public class TimeSyncMsg
    {
        public int type { get; set; }
        public uint t0 { get; set; }
        public uint t1 { get; set; }
        public uint t2 { get; set; }
    }


    public class SingleAdressedMessage : BasicMessage
    {
        public string msg { get; set; }
    }

    public class BroadcastMessage : SingleAdressedMessage { }

    public class HelperMessage
    {
        public int type { get; set; }
    }
    public class GeneralSmarthomeMessage
    {
        public uint id { get; set; }
        public string MessageType { get; set; }
        public string Command { get; set; }
        public List<string> Parameters { get; set; }
    }


    public class NodeSyncMessage : BasicMessage
    {

        [JsonConverter(typeof(SubJsonSerializer))]
        public Dictionary<uint, Sub> subs { get; set; }
    }

    public class SubJsonSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsAssignableFrom(typeof(Dictionary<uint, Sub>));
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var res = new Dictionary<uint, Sub>();

            res = serializer.Deserialize<Sub[]>(reader).ToDictionary(x => x.NodeId, x => x);

            return res;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var subs = value as Dictionary<uint, Sub>;
            serializer.Serialize(writer, subs.Values);
        }
    }

    public class Sub
    {
        [JsonProperty(PropertyName = "nodeId")]
        public uint NodeId { get; set; }
        public bool root { get; set; }

        [JsonConverter(typeof(SubJsonSerializer))]
        [JsonProperty(PropertyName = "subs")]
        public Dictionary<uint, Sub> Connections { get; set; }

        public DateTime LastReceived { get; internal set; }
        //public static explicit operator SubConnection(Sub s)
        //    => new SubConnection() { NodeId = s.nodeId, Connections = s.subs.ToDictionary(x => x.nodeId, x => (SubConnection)x) };

        public bool ContainsId(uint id)
        {
            if (NodeId == id)
                return true;
            else if (Connections.Count == 0)
                return false;
            else
                return Connections.Any(x => x.Value.ContainsId(id));
        }

        public bool RemoveConnection(Sub connection)
        {
            if (connection == null || Connections.Remove(connection.NodeId))
                return true;

            return Connections.Any(x => x.Value.RemoveConnection(connection));
        }
    }

}
