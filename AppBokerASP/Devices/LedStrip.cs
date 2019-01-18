using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace AppBokerASP.Devices
{
    public class LedStrip : Device
    {
        public string Url { get; set; }
        public LedStrip(string url)
        {
            Url = url;
            TypeName = GetType().Name;
        }

        public override void UpdateFromApp(string command, List<string> parameter)
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
