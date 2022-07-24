using AppBroker.Core.DynamicUI;

using Makaretu.Dns;

using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;

using MQTTnet.AspNetCore;

using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

using System.Net;
using System.Net.Sockets;
using System.Text;
using MQTTnet.Channel;
using System.Security.Cryptography.X509Certificates;
using MQTTnet.Adapter;
using MQTTnet.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Channels;
using MQTTnet.Formatter;
using Org.BouncyCastle.Bcpg;
using MQTTnet.Packets;
using MQTTnet.Implementations;
using NLog.Fluent;
using MQTTnet.Server;
using Microsoft.AspNetCore.Connections;

namespace AppBrokerASP;

public class Program
{
#if DEBUG
    public const bool IsDebug = true;
    private const int port = 5056;
#else
    public const bool IsDebug = false;
    private const int port = 5055;
#endif
    public static ushort UsedPortForSignalR { get; private set; }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.Unicode;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        _ = new InstanceContainer();

        _ = DeviceLayoutService.InstanceDeviceLayouts;

        Logger? mainLogger = LogManager
            .Setup()
            .LoadConfigurationFromSection(InstanceContainer.Instance.ConfigManager.Configuration)
            .GetCurrentClassLogger();

        try
        {
            if (InstanceContainer.Instance.ConfigManager.PainlessMeshConfig.Enabled)
                InstanceContainer.Instance.MeshManager.Start();

            UsedPortForSignalR = port;
            if (InstanceContainer.Instance.ConfigManager.ServerConfig.ListenPort == 0)
                mainLogger.Info($"ListenPort is not configured in the appsettings serverconfig section and therefore default port {port} will be used, when no port was passed into listen url.");
            else
                UsedPortForSignalR = InstanceContainer.Instance.ConfigManager.ServerConfig.ListenPort;

            string[] listenUrls;
            if (InstanceContainer.Instance.ConfigManager.ServerConfig.ListenUrls.Any())
            {
                listenUrls = new string[InstanceContainer.Instance.ConfigManager.ServerConfig.ListenUrls.Count];
                for (int i = 0; i < InstanceContainer.Instance.ConfigManager.ServerConfig.ListenUrls.Count; i++)
                {
                    string? item = InstanceContainer.Instance.ConfigManager.ServerConfig.ListenUrls[i];
                    try
                    {
                        var builder = new UriBuilder(item)
                        {
                            //TODO only override default ports? How do we advertise multiple ports or will there be a main port, multiple advertisments or what is the plan?
                            //if ((builder.Scheme == "http" && builder.Port == 80) || (builder.Scheme == "https" && builder.Port == 443))
                            //{
                            Port = UsedPortForSignalR
                        };
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
                listenUrls = new[] { $"http://*:{UsedPortForSignalR}" };
            }
            AdvertiseServerPortsViaMDNS(UsedPortForSignalR);

            mainLogger.Info($"Listening on urls {string.Join(",", listenUrls)}");

            WebApplicationBuilder? webBuilder = WebApplication.CreateBuilder(new WebApplicationOptions() { /*Args = args,*/ WebRootPath = "wwwroot", });
            _ = webBuilder.WebHost.UseKestrel((ks) =>
            {
                ks.ListenAnyIP(tempPort);
                ks.ListenAnyIP(InstanceContainer.Instance.ConfigManager.MqttConfig.Port, x => x.UseMqtt());
            }).UseStaticWebAssets();

            _ = webBuilder.Host.ConfigureLogging(logging =>
                       {
                           _ = logging.ClearProviders();
                           _ = logging.AddNLog();
                       });

            var startup = new Startup(webBuilder.Configuration);
            startup.ConfigureServices(webBuilder.Services);
            

            WebApplication? app = webBuilder.Build();
            _ = app.UseWebSockets();
            _ = app.UseCors("CorsPolicy");
            _ = app.UseRouting();
            _ = app.UseStaticFiles();

            _ = app.UseEndpoints(e =>
            {
                _ = e.MapFallbackToPage("/_Host");
                _ = e.MapHub<SmartHome>(pattern: "/SmartHome/{id}");
                _ = e.MapHub<SmartHome>(pattern: "/SmartHome");
                _ = e.MapControllers();

                _ = e.MapConnectionHandler<MqttConnectionHandler>(
                    "/mqtt",
                    httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                    protocolList => protocolList.FirstOrDefault() ?? string.Empty);

            });

            _ = app.UseMqttServer(server =>
            {
                // Todo: Do something with the server
            });

            app.Run();
        }
        catch (Exception ex)
        {
            // https://github.com/dotnet/MQTTnet/wiki/Server#aspnet-50=
            mainLogger.Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }


    private static void AdvertiseServerPortsViaMDNS(ushort port)
    {
        MulticastService mdns = new();
        ServiceDiscovery sd = new(mdns);

        IPAddress[]? hostEntry
            = Dns.GetHostEntry(Environment.MachineName)
            .AddressList
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork
                || (x.AddressFamily == AddressFamily.InterNetworkV6
                    && !x.IsIPv6SiteLocal
                    && !x.IsIPv6LinkLocal
                    && !x.IsIPv6UniqueLocal
                    && !x.IsIPv6Teredo))
            .ToArray();

        var serv = new ServiceProfile(InstanceContainer.Instance.ConfigManager.ServerConfig.InstanceName, "_smarthome._tcp", port, hostEntry);

        //serv.AddProperty("Min App Version", "0.0.2"); //Currently not needed, but supported by flutter app
        if (IsDebug)
            serv.AddProperty("Debug", IsDebug.ToString());
        if (string.IsNullOrWhiteSpace(InstanceContainer.Instance.ConfigManager.ServerConfig.ClusterId))
            serv.AddProperty("ClusterId", InstanceContainer.Instance.ConfigManager.ServerConfig.ClusterId);
        sd.Advertise(serv);

        mdns.Start();
    }
}

