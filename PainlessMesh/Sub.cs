using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace PainlessMesh;

public class Sub : IEquatable<Sub>
{
    //{"nodeId":1, "subs":[]}
    [JsonProperty("nodeId")]
    public uint NodeId { get; set; }

    [JsonConverter(typeof(SubJsonSerializer))]
    [JsonProperty("subs")] //{"nodeId":1,"subs":[{"nodeId":3257153498}]}
    public Dictionary<uint, Sub> Subs { get; set; }

    public override bool Equals(object obj) => Equals(obj as Sub);
    public bool Equals(Sub other) => other != null && NodeId == other.NodeId;
    public override int GetHashCode() => HashCode.Combine(NodeId);

    public static bool operator ==(Sub left, Sub right) => EqualityComparer<Sub>.Default.Equals(left, right);
    public static bool operator !=(Sub left, Sub right) => !(left == right);
}
