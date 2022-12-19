
using AppBrokerASP.Cloud;
using AppBrokerASP.Devices.Elsa;
using AppBrokerASP.SignalR;

using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;

using MQTTnet.AspNetCore;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AppBroker.Core.Javascript;
using AppBroker.Core;

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

        var signalRBuilder = services.AddSignalR(
            opt => opt.EnableDetailedErrors = true
            )
            .AddNewtonsoftJsonProtocol();


        signalRBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, NewtonsoftJsonSmarthomeHubProtocol>());

        _ = services.AddRazorPages();

        _ = services.AddApiVersioning();
        _ = services.AddSingleton<JavaScriptEngineManager>();
        var container = InstanceContainer.Instance;
        _ = services.AddSingleton(new CloudConnector());
        _ = services.AddSingleton<IInstanceContainer>(container);
        _ = services.AddSingleton(container.IconService);
        _ = services.AddSingleton(container.ConfigManager);
        _ = services.AddSingleton(container.ConfigManager.CloudConfig);
        _ = services.AddSingleton(container.ConfigManager.PainlessMeshConfig);
        _ = services.AddSingleton(container.ConfigManager.ServerConfig);
        _ = services.AddSingleton(container.ConfigManager.ZigbeeConfig);
        _ = services.AddSingleton(container.DeviceTypeMetaDataManager);
        _ = services.AddSingleton(container.DeviceManager);
        _ = services.AddSingleton(container.DeviceStateManager);

        var elsaSection = Configuration.GetSection("Elsa");

        if (elsaSection.GetValue("Enabled", true))
        {
            _ = services
             .AddElsa(options => options
                 .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                 .AddConsoleActivities()
                 .AddHttpActivities(elsaSection.GetSection("Server").Bind)
                 .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                 .AddQuartzTemporalActivities()
                 .AddJavaScriptActivities()
                 .AddFileActivities()
                 .AddPropertyActivities()
                 .AddActivitiesFrom<Startup>()
                 .AddFeatures(new[] { typeof(Startup) }, Configuration)
                 .AddWorkflowsFrom<Startup>()
                 .WithContainerName(elsaSection.GetSection("Server:ContainerName").Get<string>())
             )
            .AddJavaScriptTypeDefinitionProvider<DeviceJavascriptProvider>()
            .AddJavaScriptTypeDefinitionProvider<DefaultAppbrokerJavascriptProvider>()
            .AddNotificationHandlersFrom<DefaultAppbrokerLiquidHandler>()
            .AddNotificationHandlersFrom<DeviceLiquidHandler>();

            _ = services
                .AddElsaApiEndpoints();
        }

        if (InstanceContainer.Instance.ConfigManager.MqttConfig.Enabled)
        {
            _ = services
                .AddHostedMqttServer(mqttServer => mqttServer.WithoutDefaultEndpoint())
                .AddMqttConnectionHandler()
                .AddConnections();
        }
    }
}
