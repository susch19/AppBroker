﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using AppBrokerASP.Configuration;

using Makaretu.Dns;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using NLog;

using PainlessMesh.Ota;


namespace AppBrokerASP
{
    public class Program
    {
#if DEBUG
        public const bool IsDebug = true;
        private const int port = 5056;
#else
        public const bool IsDebug = false;
        private const int port = 5055;
#endif



        private enum PemStringType
        {
            Certificate,
            RsaPrivateKey
        }


        public static void Main(string[] args)
        {


            var mainLogger = NLog.LogManager.GetCurrentClassLogger();


            Console.OutputEncoding = Encoding.Unicode;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (InstanceContainer.ConfigManager.PainlessMeshConfig.Enabled)
                InstanceContainer.MeshManager.Start();

            ushort tempPort = port;
            if (InstanceContainer.ConfigManager.ServerConfig.ListenPort == 0)
                mainLogger.Info($"ListenPort is not configured in the appsettings serverconfig section and therefore default port {port} will be used, when no port was passed into listen url.");
            else
                tempPort = InstanceContainer.ConfigManager.ServerConfig.ListenPort;

            string[] listenUrls;
            if (InstanceContainer.ConfigManager.ServerConfig.ListenUrls.Any())
            {
                listenUrls = new string[InstanceContainer.ConfigManager.ServerConfig.ListenUrls.Count];
                for (int i = 0; i < InstanceContainer.ConfigManager.ServerConfig.ListenUrls.Count; i++)
                {
                    string? item = InstanceContainer.ConfigManager.ServerConfig.ListenUrls[i];
                    try
                    {
                        var builder = new UriBuilder(item);
                        //TODO only override default ports? How do we advertise multiple ports or will there be a main port, multiple advertisments or what is the plan?
                        //if ((builder.Scheme == "http" && builder.Port == 80) || (builder.Scheme == "https" && builder.Port == 443))
                        //{
                        builder.Port = tempPort;
                        //}
                        listenUrls[i] = builder.ToString();
                    }
                    catch (UriFormatException ex)
                    {
                        mainLogger.Error($"Error during process of {item}", ex);
                    }

                }
            }
            else
            {
                listenUrls = new[] { $"http://[::1]:{tempPort}", $"http://0.0.0.0:{tempPort}" };
            }
            AdvertiseServerPortsViaMDNS(tempPort);

            mainLogger.Info($"Listening on urls {string.Join(",", listenUrls)}");

            CreateWebHostBuilder(args).UseUrls(listenUrls).Build().Run();
        }


        private static void AdvertiseServerPortsViaMDNS(ushort port)
        {
            MulticastService mdns = new MulticastService();
            ServiceDiscovery sd = new ServiceDiscovery(mdns);

            var hostEntry 
                = Dns.GetHostEntry(Environment.MachineName)
                .AddressList
                .Where(x => x.AddressFamily == AddressFamily.InterNetwork
                    || (x.AddressFamily == AddressFamily.InterNetworkV6
                        && !x.IsIPv6SiteLocal
                        && !x.IsIPv6LinkLocal
                        && !x.IsIPv6UniqueLocal
                        && !x.IsIPv6Teredo))
                .ToArray();

            var serv = new ServiceProfile(InstanceContainer.ConfigManager.ServerConfig.InstanceName, "_smarthome._tcp", port, hostEntry);
            serv.AddProperty("Min App Version", "0.0.2");
            serv.AddProperty("Debug", IsDebug.ToString());
            sd.Advertise(serv);

            mdns.Start();
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