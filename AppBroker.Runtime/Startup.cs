
using AppBrokerASP.Cloud;
using AppBrokerASP.SignalR;

using MQTTnet.AspNetCore;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AppBroker.Core.Javascript;
using AppBroker.Core;
using AppBrokerASP.Plugins;
using Newtonsoft.Json.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using AppBroker.Main;

namespace AppBroker.Runtime;

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
            .AddNewtonsoftJson((c) =>
            {
                c.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto;
                c.SerializerSettings.MetadataPropertyHandling = Newtonsoft.Json.MetadataPropertyHandling.ReadAhead;
            })
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

        services.AddSwaggerDocument(c =>
        {
            c.RequireParametersWithoutDefault = true;

        });

        _ = services.AddSingleton<JavaScriptEngineManager>();
        var container = InstanceContainer.Instance;
        var registered = container.GetAllRegistered();
        foreach (var reg in registered)
        {
            _ = services.AddSingleton(reg.GetType(), reg);
            foreach (var item in reg.GetType().GetInterfaces().Where(x=>x.FullName.StartsWith("AppBroker")))
            {
                services.AddSingleton(item, reg);
            }
        }
        //_ = services.AddSingleton(new CloudConnector());
        _ = services.AddSingleton<IInstanceContainer>(container);
        _ = services.AddSingleton(container.ServerConfigManager.CloudConfig);
        _ = services.AddSingleton(container.ServerConfigManager.ServerConfig);

        if (InstanceContainer.Instance.ConfigManager.MqttConfig.Enabled)
        {
            _ = services
                .AddHostedMqttServer(mqttServer => mqttServer.WithoutDefaultEndpoint())
                .AddMqttConnectionHandler()
                .AddConnections();
        }
    }
}
