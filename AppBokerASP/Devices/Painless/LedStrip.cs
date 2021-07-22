using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;

using AppBokerASP.Extension;

using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json.Linq;

using PainlessMesh;

namespace AppBokerASP.Devices.Painless
{//2021-01-08 22:39:51.9215|DEBUG|AppBokerASP.BaseClient|{"id":763955710, "m":"Update", "c":"Mode", "p":["SingleColor",16,94,239,86,0,4278190080,1]}
    [PainlessMeshName("ledstri")]
    public class LedStrip : PainlessDevice
    {
        //{"id":763955710, "m":"Update", "c":"Mode", "p":["SingleColor",55,93,88,30,0,4278190080,1]}

        public string ColorMode { get; set; }
        public int Delay { get; set; }
        public int NumberOfLeds { get; set; }
        public int Brightness { get; set; }
        public uint Step { get; set; }
        public bool Reverse { get; set; }
        public uint ColorNumber { get; set; }
        public ushort Version { get; set; }

        public LedStrip(long id, List<string> parameter) : base(id, parameter)
        {
            ShowInApp = true;

            Program.MeshManager.SingleUpdateMessageReceived += SingleUpdateMessageReceived;
        }

        private void SingleUpdateMessageReceived(object? sender, GeneralSmarthomeMessage e)
        {
            if (e.Command != Command.Mode)
                return;

            for (int i = 0; i < e.Parameters.Count; i++)
            {
                JToken? item = e.Parameters[i];
                if (item is null)
                    continue;
                var str = item.ToString();
                if (i == 0)
                    ColorMode = str;
                else if (i == 1 && int.TryParse(str, out var delay))
                    Delay = delay;
                else if (i == 2 && int.TryParse(str, out var numled))
                    NumberOfLeds = numled;
                else if (i == 3 && int.TryParse(str, out var brightness))
                    Brightness = brightness;
                else if (i == 4 && uint.TryParse(str, out var step))
                    Step = step;
                else if (i == 5 && bool.TryParse(str, out var reverse))
                    Reverse = reverse;
                else if (i == 6 && uint.TryParse(str, out var color))
                    ColorNumber = color;
                else if (i == 7 && ushort.TryParse(str, out var version))
                    Version = version;
            }

            //var param = e.Parameters.FirstOrDefault();


            //logger.Debug(param.ToString());

            //var span = param.ToString().AsSpan();
            //var indices = span.IndexesOf(',');

            

            SendDataToAllSubscribers();
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

        public override void Reconnect(List<string>? parameter)
        {

            Program.MeshManager.SingleUpdateMessageReceived += SingleUpdateMessageReceived;
            base.Reconnect(parameter);
        }

        public override void StopDevice()
        {
            Program.MeshManager.SingleUpdateMessageReceived -= SingleUpdateMessageReceived;
            base.StopDevice();
        }

    }
}
