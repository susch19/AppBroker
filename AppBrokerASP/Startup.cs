
using AppBrokerASP.Cloud;
using AppBrokerASP.SignalR;

using MQTTnet.AspNetCore;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AppBroker.Core.Javascript;
using AppBroker.Core;
using AppBrokerASP.Plugins;

namespace AppBrokerASP;

public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        _ = services.Configure<CookiePolicyOptions>(options =>
                  // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                  options.MinimumSameSitePolicy = SameSiteMode.None);

        _ = services.AddCors(options => options.AddPolicy("CorsPolicy", builder => _ = builder
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowAnyOrigin()));

        services
            .AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new GenericControllerFeatureProvider());
            });

        var signalRBuilder = services.AddSignalR(
            opt =>
            {
                opt.EnableDetailedErrors = true;
            }
            )
            .AddNewtonsoftJsonProtocol();


        signalRBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NewtonsoftJsonSmarthomeHubProtocol>());

        _ = services.AddRazorPages();

        _ = services.AddSingleton<JavaScriptEngineManager>();
        var container = InstanceContainer.Instance;
        _ = services.AddSingleton(new CloudConnector());
        _ = services.AddSingleton<IInstanceContainer>(container);
        _ = services.AddSingleton(container.IconService);
        _ = services.AddSingleton(container.ConfigManager);
        _ = services.AddSingleton(container.ServerConfigManager.CloudConfig);
        _ = services.AddSingleton(container.ServerConfigManager.ServerConfig);
        _ = services.AddSingleton(container.DeviceTypeMetaDataManager);
        _ = services.AddSingleton(container.DeviceManager);
        _ = services.AddSingleton(container.DeviceStateManager);
        _ = services.AddSingleton(container.HistoryManager);



        if (InstanceContainer.Instance.ConfigManager.MqttConfig.Enabled)
        {
            _ = services
                .AddHostedMqttServer(mqttServer => mqttServer.WithoutDefaultEndpoint())
                .AddMqttConnectionHandler()
                .AddConnections();
        }
    }
}
