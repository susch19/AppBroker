﻿using AppBroker.Core.DynamicUI;

using Makaretu.Dns;

using MQTTnet.AspNetCore;

using NLog;
using NLog.Extensions.Logging;
using NLog.Web;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;
using AppBrokerASP.Plugins;
using AppBroker.Core;
using MQTTnet.Server;
using MQTTnet;
using Newtonsoft.Json;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;

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
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var pluginLoader = new PluginLoader(LogManager.LogFactory);
        pluginLoader.LoadAssemblies();
        _ = new InstanceContainer(pluginLoader);

        _ = DeviceLayoutService.InstanceDeviceLayouts;

        Logger? mainLogger = LogManager
            .Setup()
            .LoadConfigurationFromSection(InstanceContainer.Instance.ConfigManager.Configuration)
            .GetCurrentClassLogger();

        try
        {

            UsedPortForSignalR = port;
            if (InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenPort == 0)
                mainLogger.Info($"ListenPort is not configured in the appsettings serverconfig section and therefore default port {port} will be used, when no port was passed into listen url.");
            else
                UsedPortForSignalR = InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenPort;

            string[] listenUrls;
            if (InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenUrls.Any())
            {
                listenUrls = new string[InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenUrls.Count];
                for (int i = 0; i < InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenUrls.Count; i++)
                {
                    string? item = InstanceContainer.Instance.ServerConfigManager.ServerConfig.ListenUrls[i];
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

            WebApplicationBuilder? webBuilder = WebApplication
                .CreateBuilder(new WebApplicationOptions() { Args = args, WebRootPath = "wwwroot" });

            _ = webBuilder.Host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();
                _ = config.AddConfiguration(InstanceContainer.Instance.ConfigManager.Configuration);
            })
            ;


            _ = webBuilder.WebHost.UseKestrel((ks) =>
            {
                foreach (var url in listenUrls)
                {
                    var uri = new Uri(url);
                    ks.Listen(CreateIPEndPoint(uri.Host + ":" + uri.Port));
                }

                if (InstanceContainer.Instance.ConfigManager.MqttConfig.Enabled)
                {
                    ks.ListenAnyIP(InstanceContainer.Instance.ConfigManager.MqttConfig.Port, x => x.UseMqtt());
                }
            });

            _ = webBuilder.Host.ConfigureLogging(logging =>
            {
                _ = logging.ClearProviders();
                _ = logging.AddNLog();
            });
            var mqttConfig = InstanceContainer.Instance.ConfigManager.MqttConfig;

            var startup = new Startup(webBuilder.Configuration);
            startup.ConfigureServices(webBuilder.Services);
            List<Type> hubTypes = new();
            foreach (var extender in pluginLoader.ServiceExtenders)
            {
                extender.ConfigureServices(webBuilder.Services);
                foreach (var type in extender.GetHubTypes())
                {
                    hubTypes.Add(type);
                }
            }

            WebApplication? app = webBuilder.Build();
            _ = app.UseWebSockets();
            _ = app.UseCors("CorsPolicy");
            _ = app.UseRouting();
            _ = app.UseStaticFiles();

            Type dynamicHub = GenerateDynamicHub(hubTypes, mainLogger);
            _ = app.UseEndpoints(e =>
            {
                //_ = e.MapFallbackToPage("/_Host");
                _ = e.MapControllers();
                var mapHubMethod = typeof(HubEndpointRouteBuilderExtensions).GetMethod("MapHub", 1, new[] { typeof(IEndpointRouteBuilder), typeof(string) });
                _ = mapHubMethod.MakeGenericMethod(dynamicHub).Invoke(null, new object[] { e, "/Smarthome" });
                _ = mapHubMethod.MakeGenericMethod(dynamicHub).Invoke(null, new object[] { e, "/Smarthome/{id}" });

                foreach (var extender in pluginLoader.ServiceExtenders)
                {
                    extender.UseEndpoints(e);
                }

                if (mqttConfig.Enabled)
                {
                    _ = e.MapConnectionHandler<MqttConnectionHandler>(
                        "/MQTTClient",
                        httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                        protocolList => protocolList.FirstOrDefault() ?? string.Empty);
                }
            });

            if (mqttConfig.Enabled)
            {
                _ = app.UseMqttServer(server =>
                {
                    static async Task Server_RetainedMessagesClearedAsync(EventArgs arg) => File.Delete(InstanceContainer.Instance.ConfigManager.MqttConfig.RetainedMessageFilePath);
                    static Task Server_LoadingRetainedMessageAsync(LoadingRetainedMessagesEventArgs arg)
                    {
                        if (File.Exists(InstanceContainer.Instance.ConfigManager.MqttConfig.RetainedMessageFilePath))
                        {
                            var json = File.ReadAllText(InstanceContainer.Instance.ConfigManager.MqttConfig.RetainedMessageFilePath);
                            arg.LoadedRetainedMessages = JsonConvert.DeserializeObject<List<MqttApplicationMessage>>(json);
                        }
                        else
                        {
                            arg.LoadedRetainedMessages = new List<MqttApplicationMessage>();
                        }
                        return Task.CompletedTask;
                    }

                    static async Task Server_RetainedMessageChangedAsync(RetainedMessageChangedEventArgs arg) => File.WriteAllText(InstanceContainer.Instance.ConfigManager.MqttConfig.RetainedMessageFilePath, JsonConvert.SerializeObject(arg.StoredRetainedMessages));
                    ;


                    server.RetainedMessageChangedAsync += Server_RetainedMessageChangedAsync;
                    server.LoadingRetainedMessageAsync += Server_LoadingRetainedMessageAsync;
                    server.RetainedMessagesClearedAsync += Server_RetainedMessagesClearedAsync;
                    // Todo: Do something with the server
                    // https://github.com/dotnet/MQTTnet/wiki/Server#aspnet-50=
                });
            }
            if (InstanceContainer.Instance.ServerConfigManager.ServerConfig.EnableJavaScript)
            {
                InstanceContainer.Instance.JavaScriptEngineManager.Initialize();
            }
            if (app.Environment.IsDevelopment())
            {
                //app.UseSwagger();
                //app.UseSwaggerUI();
                app.UseOpenApi(c =>
                {
                }); // serve documents (same as app.UseSwagger())
                //app.UseSwaggerUi3(); // serve Swagger UI
                app.UseReDoc(); // serve ReDoc UI
            }
            pluginLoader.InitializePlugins(LogManager.LogFactory);

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


    private static IPEndPoint CreateIPEndPoint(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length < 2)
            throw new FormatException("Invalid endpoint format");

        IPAddress? ip;
        if (ep.Length > 2)
        {
            if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                throw new FormatException("Invalid ip-adress");
        }
        else
        {
            if (!IPAddress.TryParse(ep[0], out ip))
                throw new FormatException("Invalid ip-adress");
        }

        if (!int.TryParse(ep[^1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
            throw new FormatException("Invalid port");

        return new IPEndPoint(ip, port);
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

        var serv = new ServiceProfile(InstanceContainer.Instance.ServerConfigManager.ServerConfig.InstanceName, "_smarthome._tcp", port, hostEntry);

        //serv.AddProperty("Min App Version", "0.0.2"); //Currently not needed, but supported by flutter app
        if (IsDebug)
            serv.AddProperty("Debug", IsDebug.ToString());
        if (string.IsNullOrWhiteSpace(InstanceContainer.Instance.ServerConfigManager.ServerConfig.ClusterId))
            serv.AddProperty("ClusterId", InstanceContainer.Instance.ServerConfigManager.ServerConfig.ClusterId);
        sd.Advertise(serv);

        mdns.Start();
    }

    private static Type GenerateDynamicHub(List<Type> hubTypes, Logger mainLogger)
    {
        AssemblyName assemblyName = new AssemblyName("DynamicHubAssembly");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        TypeBuilder typeBuilder = moduleBuilder.DefineType("RuntimeHub", TypeAttributes.Public, typeof(DynamicHub));
        foreach (var hubType in hubTypes)
        {
            foreach (var method in hubType.GetMethods())
            {
                if (method.DeclaringType != hubType || (method.Attributes & MethodAttributes.Private) > 0)
                    continue;
                if ((method.Attributes & MethodAttributes.Static) == 0)
                {
                    mainLogger.Warn("Method {0}.{1} was not static, only public static methods are supported", hubType.FullName, method.Name);
                    continue;
                }
                var parameters = method.GetParameters();
                bool passThis = false;
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(DynamicHub))
                    passThis = true;
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig,
                    method.ReturnType,
                    parameters.Skip(passThis ? 1 : 0).Select(x => x.ParameterType).ToArray());

                var gen = methodBuilder.GetILGenerator();

                for (int i = 0; i < parameters.Length; i++)
                {
                    gen.Emit(OpCodes.Ldarg, i + (passThis ? 0 : 1));
                }
                gen.EmitCall(OpCodes.Call, method, null);
                gen.Emit(OpCodes.Ret);
            }
        }
        Type dynamicType = typeBuilder.CreateType();
        return dynamicType;
    }
}
