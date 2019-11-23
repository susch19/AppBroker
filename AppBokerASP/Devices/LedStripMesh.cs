using PainlessMesh;

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace AppBokerASP.Devices
{
    public class LedStripMesh : Device
    {
        public LedStripMesh() : base(0)
        {
        }

        public LedStripMesh(uint id) : base(id)
        {
            TypeName = GetType().Name;
            //Program.MeshManager.SingleUpdateMessageReceived += Node_SingleMessageReceived;
        }

        private void Node_SingleMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
        }

        public override void UpdateFromApp(Command command, List<JsonElement> parameter)
        {
            string args = "";

            //if (command == Command. "options")
            //{
            //    var ledRO = new LedStripRootObject { id = Id, MessageType = command, OptionSet = new List<OptionSet>() };
            //    foreach (var item in parameter)
            //    {
            //        ledRO.OptionSet.Add(new OptionSet { Option = item.Split('=')[0], Value = item.Split('=')[1] });
            //    }

            //    Program.MeshManager.SendSingle(Id, ledRO);
            //}
            //else
            //{
            //    Rootobject ro = new Rootobject
            //    {
            //        id = Id,
            //        MessageType = command
            //    };
            //    Program.MeshManager.SendSingle(Id, ro);

            //}

            //if (parameter != null)
            //    args = string.Join("&", parameter);

            //var wr = WebRequest.Create(new Uri($"{Url}/{command}{(args == "" ? "" : "?")}{args}"));
            //wr.Method = "GET";
            //wr.GetResponse();
        }

        private class LedStripRootObject : Rootobject
        {
            public List<OptionSet> OptionSet { get; set; }
        }

        private class OptionSet
        {
            public string Option { get; set; }
            public string Value { get; set; }
        }

    }

}
