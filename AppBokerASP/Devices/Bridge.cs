using Newtonsoft.Json.Linq;
using PainlessMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppBokerASP.Devices
{
    public class Bridge : Device
    {
        public Bridge(long nodeId, List<string> parameter) : base(nodeId) => ShowInApp = false;
    }
}
