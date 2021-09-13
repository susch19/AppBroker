using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

using AppBrokerASP.Configuration;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using PainlessMesh.Ota;


namespace AppBrokerASP
{
    public class Program
    {
   


        private enum PemStringType
        {
            Certificate,
            RsaPrivateKey
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




            //Dostuff();
            if (InstanceContainer.ConfigManager.PainlessMeshConfig.Enabled)
                InstanceContainer.MeshManager.Start();

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
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://0.0.0.0:5056").Build().Run();
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
