using AppBroker.Core.DynamicUI;

using Makaretu.Dns;

using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

using System.Net;
using System.Net.Sockets;
using System.Text;

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

    static SemaphoreSlim slim = new SemaphoreSlim(1, 1);

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

            ushort tempPort = port;
            if (InstanceContainer.Instance.ConfigManager.ServerConfig.ListenPort == 0)
                mainLogger.Info($"ListenPort is not configured in the appsettings serverconfig section and therefore default port {port} will be used, when no port was passed into listen url.");
            else
                tempPort = InstanceContainer.Instance.ConfigManager.ServerConfig.ListenPort;

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
                            Port = tempPort
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
                listenUrls = new[] { $"http://*:{tempPort}" };
            }
            AdvertiseServerPortsViaMDNS(tempPort);

            mainLogger.Info($"Listening on urls {string.Join(",", listenUrls)}");

            WebApplicationBuilder? webBuilder = WebApplication.CreateBuilder(new WebApplicationOptions() { /*Args = args,*/ WebRootPath = "wwwroot", });
            _ = webBuilder.WebHost.UseKestrel((ks) =>
            {
                ks.ListenAnyIP(tempPort);
            });

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

            });



            _ = Task.Run(async () =>
            {
                ConcurrentDictionary<TcpClient, TcpClient> tcpClients = new();

                byte[] firstMessage = new byte[1];
                int i = 0;
                const string endpoint = "DESKTOP-ACHBV7O";
                const ushort port = 5057;
                void OnClientConnect(IAsyncResult x)
                {
                    var client = (TcpClient)x.AsyncState!;
                    client.EndConnect(x);
                    mainLogger.Warn("Busy Waiting for new Connection >>>>>>>>>>>>> " + i);
                    _ = AcceptNewClient(client, mainLogger, tcpClients, firstMessage, i++).Result;
                    client = new TcpClient();
                    mainLogger.Warn("Waiting for TCP Connection >>>>>>>>>>>>> " + i);
                    client.BeginConnect(endpoint, port, OnClientConnect, client);
                }

                var client = new TcpClient();
                mainLogger.Warn("Waiting for TCP Connection >>>>>>>>>>>>> " + i);
                client.BeginConnect(endpoint, port, OnClientConnect, client);
            });

            app.Run();
        }
        catch (Exception ex)
        {
            mainLogger.Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            NLog.LogManager.Shutdown();
        }
    }

    private static async Task<int> AcceptNewClient(TcpClient x, Logger mainLogger, ConcurrentDictionary<TcpClient, TcpClient> tcpClients, byte[] firstMessage, int i)
    {
        mainLogger.Warn("Starting new connection >>>>>>>>>>>>> " + i++);
        var incomming = x;

        var incommingStr = incomming.GetStream();

        incommingStr.Write(Encoding.UTF8.GetBytes("/SmartHome/1234567/Server"));

        incommingStr.ReadExactly(firstMessage);

        var self = new TcpClient("localhost", 5056);
        self.NoDelay = true;
        var selfStr = self.GetStream();
        tcpClients[incomming] = self;

        _ = Task.Run(async () =>
        {

            mainLogger.Warn("Server started >>>>>>>>>>>>> " + (i - 1));
            while (true)
            {
                try
                {

                    if (self.Available > 0)
                    {
                        var bytes = new byte[self.Available];
                        selfStr.ReadExactly(bytes);
                        mainLogger.Warn($"Send back {bytes.Length}");
                        var bp = System.Text.Encoding.UTF8.GetString(bytes);
                        incommingStr.Write(bytes);
                    }
                }
                catch (Exception ex)
                {
                    mainLogger.Warn($"Send error (Client:{incomming.Connected}, Self:{self.Connected}) {ex}");
                    tcpClients.Remove(incomming, out _);
                    incomming?.Close();
                    self?.Close();
                    break;

                }
                await Task.Delay(1);
            }
        });
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    if (incomming.Available > 0)
                    {
                        var bytes = new byte[incomming.Available];
                        incommingStr.ReadExactly(bytes);
                        var bp = System.Text.Encoding.UTF8.GetString(bytes);
                        mainLogger.Warn($"Rec {bytes.Length}");
                        selfStr.Write(bytes);

                    }
                }
                catch (Exception ex)
                {
                    mainLogger.Warn($"Rec error (Client:{incomming.Connected}, Self:{self.Connected}) {ex}");

                    tcpClients.Remove(incomming, out _);
                    incomming?.Close();
                    self?.Close();
                    break;
                }
                await Task.Delay(1);
            }
        });
        return i;
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

