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
        public Bridge(uint nodeId) : base(nodeId)
        {
        }

        public override void UpdateFromApp(Command command, List<JsonElement> parameter) => throw new NotImplementedException();
    }
}
