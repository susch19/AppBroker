using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;

namespace AppBokerASP.Devices
{

    public class LedStrip : Device
    {
        public string Url { get; set; }
        public LedStrip(string url) : base(0) 
        {
            Url = url;
            TypeName = GetType().Name;
        }

        public override void UpdateFromApp(Command command, List<JsonElement> parameter)
        {
            string args = "";

            if (parameter != null)
                args = string.Join("&", parameter);

            var wr = WebRequest.Create(new Uri($"{Url}/{command}{(args == "" ? "" : "?")}{args}"));
            wr.Method = "GET";
            wr.GetResponse();
        }

    }
}
