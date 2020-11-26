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
        public string CurrentMode { get; set; }
        public string CurrentParams { get; set; }

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

        public override void StopDevice()
        {
            base.StopDevice();
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
        }

        private void Node_SingleOptionsMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
        }

        private void Node_SingleUpdateMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
        }

        public override void Reconnect()
        {
            base.Reconnect();
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
        }
    }
}
