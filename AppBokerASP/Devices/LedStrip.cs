using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using PainlessMesh;

namespace AppBokerASP.Devices
{

    public class LedStrip : Device
    {
        public LedStrip(long id, List<string> parameter) : base(id)
        {
            ShowInApp = true;
        }

        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {
            var msg = new GeneralSmarthomeMessage(Id, MessageType.Update, command, parameters.ToArray());
            Program.MeshManager.SendSingle(Id, msg);
        }

        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {
            var msg = new GeneralSmarthomeMessage(Id, MessageType.Options, command, parameters.ToArray());
            Program.MeshManager.SendSingle(Id, msg);
        }
    }
}
