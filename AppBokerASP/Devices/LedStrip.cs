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
        public LedStrip(uint id) : base(id)
        {
            ShowInApp = true;
        }

        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {
            var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Update, command, parameters.ToArray());
            Program.MeshManager.SendSingle((uint)Id, msg);
        }

        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {
            var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, command, parameters.ToArray());
            Program.MeshManager.SendSingle((uint)Id, msg);
        }
    }
}
