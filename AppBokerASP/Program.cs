using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PainlessMesh;
using Websocket.Client;

namespace AppBokerASP
{
    public class Program
    {
        public static DeviceManager DeviceManager { get; private set; }

        public static SmarthomeMeshManager MeshManager { get; private set; }

        private static Task t;
        private static WebsocketClient wsc;

        //private static TcpListener TcpServer = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), 8801);

        public static void Main(string[] args)
        {
            MeshManager = new SmarthomeMeshManager(8801);
            //MeshManager.Start();

            var s = "{\"id\":3821496450,\"MessageType\":\"OnNewConnection\",\"Command\":\"OnNewConnection\",\"Parameters\":[{\"nodeId\": 1122111222}]}";

            var m = System.Text.Json.JsonSerializer.Deserialize<GeneralSmarthomeMessage>(s /*jso*/);

            DeviceManager = new DeviceManager();
            Dostuff();

            int asd = 123;
            var bytes = BitConverter.GetBytes(asd);
            if (args.Length > 0)
            {
                CreateWebHostBuilder(args).UseUrls(args).Build().Run();
            }
            else
            {
#if DEBUG
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://192.168.49.28:5056").Build().Run();
#else
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5055", "http://192.168.49.71:5055").Build().Run();
#endif
            }
        }

      

        private static async void Dostuff()
        {
            return;
            var wc = WebRequest.Create(@"https://192.168.49.71:8087/objects?pattern=system.adapter.zigbee.0*&prettyPrint");

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            using (var sr = new StreamReader(wc.GetResponse().GetResponseStream()))
            {
                Console.WriteLine(sr.ReadToEnd());
            }
            ClientWebSocket cws = new ClientWebSocket();
            cws.Options.RemoteCertificateValidationCallback = ServicePointManager.ServerCertificateValidationCallback;
            await cws.ConnectAsync(new Uri("wss://192.168.49.71:8084"), CancellationToken.None);
            //wsc = new WebsocketClient(new Uri("ws://192.168.49.71:8084"));
            //wsc.MessageReceived.Subscribe((s) =>
            //s.Clone());
            //wsc.Start();
            //while(!wsc.IsRunning)
            //{
            //    Thread.Sleep
            //}
            //await wsc.Send("getStates");
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>();
    }
}
