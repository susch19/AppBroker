using System;
using System.Net;

namespace AppBroker.Devices
{
    public class LedStrip : Device
    {
        public override void Update(string command, string[] parameter)
        {
            var wr = WebRequest.Create(new Uri($"{Url}/{command}/?{parameter}"));
            wr.Method = "GET";
            wr.GetResponse();
        }
    }
}
