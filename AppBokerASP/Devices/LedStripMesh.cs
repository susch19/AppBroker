using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PainlessMesh;

namespace AppBokerASP.Devices
{
    public class LedStripMesh : Device
    {
        public LedStripMesh(uint id)
        {
            Id = id;
            TypeName = GetType().Name;
            Program.Node.SingleMessageReceived += Node_SingleMessageReceived;
        }

        private void Node_SingleMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
        }

        public override void UpdateFromApp(string command, List<string> parameter)
        {
            //string args = "";
            if (command == "options")
            {
                var ledRO = new LedStripRootObject { id = Id, MessageType = command, OptionSet = new List<OptionSet>() };
                foreach (var item in parameter)
                {
                    ledRO.OptionSet.Add(new OptionSet { Option = item.Split('=')[0], Value = item.Split('=')[1] });
                }

                Program.Node.SendSingle(Id, JsonConvert.SerializeObject(ledRO));
            }
            else
            {
                Rootobject ro = new Rootobject
                {
                    id = Id,
                    MessageType = command
                };
                Program.Node.SendSingle(Id, JsonConvert.SerializeObject(ro));

            }

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
