using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AppBokerASP.Database;
using AppBokerASP.Database.Model;
using AppBokerASP.Devices;
using AppBokerASP.Devices.Heater;
using AppBokerASP.IOBroker;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PainlessMesh;
using SimpleSocketIoClient;

namespace AppBokerASP
{
    public class Program
    {
        public static DeviceManager DeviceManager { get; private set; }

        public static SmarthomeMeshManager MeshManager { get; private set; }

        private static Task t;

        //private static TcpListener TcpServer = new TcpListener(new IPAddress(new byte[] { 0, 0, 0, 0 }), 8801);


        /*
         * 
         * 
    [JsonPropertyName("id"), nj.JsonProperty("id")]
    public uint NodeId { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter)), JsonPropertyName("m"), nj.JsonProperty("m")]
    public MessageType MessageType { get; set; }
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter)), JsonPropertyName("c"), nj.JsonProperty("c")]
    public Command Command { get; set; }
    [JsonPropertyName("p"), nj.JsonProperty("p")/*, nj.JsonConverter(typeof(SingleOrListConverter<JsonElement>))]
    public List<JObject> Parameters { get; set; }
    public GeneralSmarthomeMessage(uint nodeId, MessageType messageType, Command command, params JObject[] parameters)
    {
        NodeId = nodeId;
        MessageType = messageType;
        Command = command;
        Parameters = parameters.ToList();
    }
    public GeneralSmarthomeMessage()
    {
    }
         * 
         * 
         * 
         */
        public static void Main(string[] args)
        {
            //TimeTempMessageLE.Test();
            //var ttm = new TimeTempMessageLE(Devices.Heater.DayOfWeek.Sat, TimeSpan.FromMinutes(1985), 55.5f);
            //Console.WriteLine(ttm.GetBits());
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Dostuff();
            MeshManager = new SmarthomeMeshManager(8801);
            DeviceManager = new DeviceManager();

            int asd = 123;
            var bytes = BitConverter.GetBytes(asd);
            if (args.Length > 0)
            {
                CreateWebHostBuilder(args).UseUrls(args).Build().Run();
            }
            else
            {
#if DEBUG
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://0.0.0.0:5056").Build().Run();
#else
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5055", "http://0.0.0.0:5055").Build().Run();
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
