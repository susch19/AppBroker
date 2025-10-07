using AppBroker.Core;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Windmill.Model
{
    public class WindmillSmarthomeMessage : BaseSmarthomeMessage
    {
        public List<JToken> Parameters { get; set; }

        [JsonProperty("id")]
        public override long NodeId { get; set; }

        [JsonProperty("idHex")]
        public string NodeIdHex { get; set; } = "";
    }
}
