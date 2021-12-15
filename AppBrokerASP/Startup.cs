
using AppBrokerASP.Devices.Elsa;

using AspNetCore.RouteAnalyzer;

using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        _ = services.AddSignalR(
            opt => opt.EnableDetailedErrors = true
            ).AddNewtonsoftJsonProtocol();
        _ = services.AddRazorPages();

        var elsaSection = Configuration.GetSection("Elsa");

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

}
