//using System;
//using System.Collections.Generic;
//using AppBroker.Devices;
//using WebSocketSharp;
//using WebSocketSharp.Server;

//namespace AppBroker
//{
//    internal class Program
//    {
//        static Dictionary<int, Device> devices = new Dictionary<int, Device>();
//        public class SmartHome : WebSocketBehavior
//        {


//            protected override void OnMessage(MessageEventArgs e)
//            {

//            }
//        }

//        private static void Main(string[] args)
//        {
//            devices.Add(1, new LedStrip() {Id=1, Url= "http://192.168.49.57" });
//            var server = new WebSocketServer(43321);
//            server.AddWebSocketService<SmartHome>("/SmartHome");
//            server.Start();

//            Console.ReadLine();
//        }
//    }
//}

using SuperSocket;
using SuperSocket.ProtoBase;
using SuperSocket.Server;
static class Program
{
    public static void Main(string[] args)
    {
        var server = CreateSocketServer<LinePackageInfo, LinePipelineFilter>(packageHandler: async (s, p) =>
        {
            await s.SendAsync(Encoding.UTF8.GetBytes(p.Line).AsReadOnlySpan());
        });
    }
}