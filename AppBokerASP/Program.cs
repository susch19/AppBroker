using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://192.168.49.28:5056").Build().Run();
#else
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5055", "http://192.168.49.71:5055").Build().Run();
#endif
            }
        }

        private static string GetGatewayAddress()
        {

            var ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name == "wlan0");
            string connectionUrl;
            if (ni != null)
            {
                connectionUrl = ni.GetIPProperties()?.GatewayAddresses?.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork)?.Address?.ToString();
            }
            else
            {
                connectionUrl = "10.124.187.1";//10.12.206.1, 10.9.254.1
            }
            return connectionUrl;
        }

        private static async void DoStuff()
        {
            object locking = new object();

            string connectionUrl = "";
            bool changed = false;
            NetworkChange.NetworkAddressChanged += (s, e) =>
            {
                lock (locking)
                {
                    try
                    {
                        connectionUrl = GetGatewayAddress();
                        changed = true;
                    }
                    catch (Exception)
                    {
                    }
                }
            };

            while (true)
            {
                try
                {
                    lock (locking)
                    {
                        connectionUrl = GetGatewayAddress();
                    }
                    await Node.ConnectTCPAsync(connectionUrl, 5555);
                    //await Node.ConnectTCPAsync("10.9.254.1", 5555);
                    Console.WriteLine("Connection sucessful: " + connectionUrl);
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Connection failed: " + connectionUrl);
                    Thread.Sleep(250);
                }
            }
            new Task(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (Node.Client == null || Node.Client.Connected == false || changed)
                        {
                            //await Node.ConnectTCPAsync("10.9.254.1", 5555);
                            lock (locking)
                            {
                                changed = false;
                            }
                            await Node.ConnectTCPAsync(connectionUrl, 5555);
                            Console.WriteLine("Reconnect sucessful: " + connectionUrl);
                        }
                    }
                    catch (Exception)
                    {
                        lock (locking)
                        {
                            connectionUrl = GetGatewayAddress();
                            Console.WriteLine("Connection aborted, reconnecting: " + connectionUrl);
                        }
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
