using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PainlessMesh;

namespace AppBokerASP
{
    public class Program
    {
        public static Node Node { get; private set; }
        public static DeviceManager DeviceManager { get; private set; }

        public static void Main(string[] args)
        {
            Node = new Node(120);
            DeviceManager = new DeviceManager();
            DoStuff();

            if (args.Length > 0)
            {
                CreateWebHostBuilder(args).UseUrls(args).Build().Run();
            }
            else
            {
#if DEBUG
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5050", "http://*:5050").Build().Run();
#else
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5050", "http://127.0.0.1:5050").Build().Run();
#endif
            }
        }

        private static async void DoStuff()
        {
            string connectionUrl = "10.21.143.1";//10.12.206.1, 10.9.254.1
            while (true)
            {
                try
                {
                    await Node.ConnectTCPAsync(connectionUrl, 5555);
                    //await Node.ConnectTCPAsync("10.9.254.1", 5555);
                    Console.WriteLine("Connection sucessful");
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Connection failed");

                }
            }
            new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Node.Client == null || Node.Client.Connected == false)
                        {
                            //await Node.ConnectTCPAsync("10.9.254.1", 5555);
                            await Node.ConnectTCPAsync(connectionUrl, 5555);
                            Console.WriteLine("Reconnect sucessful");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Connection aborted, reconnecting");

                    }
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private static void Node_SingleMessageReceived(object sender, string e) => Console.WriteLine(e);

        private static void Node_BroadcastMessageReceived(object sender, string e) => Console.WriteLine(e);

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                WebHost.CreateDefaultBuilder(args)
                    .UseStartup<Startup>();
    }
}
