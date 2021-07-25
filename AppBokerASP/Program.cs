using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;


using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NLog;

using PainlessMesh;
using PainlessMesh.Ota;


namespace AppBokerASP
{
    public class Program
    {
        public static DeviceManager DeviceManager { get; private set; }

        public static SmarthomeMeshManager MeshManager { get; private set; }
        public static UpdateManager UpdateManager { get; private set; }

        //private static NLog.Logger Logger { get; } = NLog.LogManager.GetCurrentClassLogger();



        //private static Task t;

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
        private enum PemStringType
        {
            Certificate,
            RsaPrivateKey
        }

        static Program()
        {
            UpdateManager = new();
            MeshManager = new SmarthomeMeshManager(8801);
            DeviceManager = new DeviceManager();
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //string s = "\"SingleColor\",55,93,88,30,0,4278190080,1";

            //var span = s.AsSpan();


            //var jtoken = $"{{\"Date\":\"{DateTime.Now:dd.MM.yyyy HH:mm:ss}\"}}".ToJToken();
            //var shm = new GeneralSmarthomeMessage(0, MessageType.Update, Command.Time, $"{{\"Date\":\"{DateTime.Now:dd.MM.yyyy HH:mm:ss}\"}}".ToJToken());

            //ConfigureLogger();




            Dostuff();
     

            MeshManager.Start();

            //{"id":3257171131, "m":"Update", "c":"WhoIAm", "p":["10.9.254.4","heater","jC7/P5Uu/z+Y"]}
#if DEBUG
            //MeshManager.SocketClientDataReceived(null, new GeneralSmarthomeMessage(3257171131, MessageType.Update, Command.WhoIAm, JToken.Parse("\"10.9.254.4\""), JToken.Parse("\"heater\"")));
#endif

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

        private static void ConfigureLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = Path.Combine("Logs", $"{DateTime.Now:yyyy_MM_dd}.log") };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logconsole);
            config.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
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
            ClientWebSocket cws = new();
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
            WebHost
            .CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                //logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                //logging.AddFilter("Microsoft.AspNetCore.SignalR", Microsoft.Extensions.Logging.LogLevel.Trace);
                //    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", Microsoft.Extensions.Logging.LogLevel.Trace);
            })
            .UseStartup<Startup>();
    }
}
