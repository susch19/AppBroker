
using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppBrokerASP
{
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
              {
                  // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                  options.MinimumSameSitePolicy = SameSiteMode.None;
              });

            _ = services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
              {
                  _ = builder
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowAnyOrigin();
              }));

            _ = services.AddSignalR(
                opt => opt.EnableDetailedErrors = true
                ).AddNewtonsoftJsonProtocol();

            var elsaSection = Configuration.GetSection("Elsa");


            _ = services
                 .AddElsa(options => options
                     //.UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                     .AddConsoleActivities()
                     //.AddHttpActivities(elsaSection.GetSection("Server").Bind)
                     //.AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                     //.AddQuartzTemporalActivities()
                     .AddJavaScriptActivities()
                     //.AddFileActivities()
                     .AddPropetyActivities()
                     .AddActivitiesFrom<Startup>()
                     .AddFeatures(new[] { typeof(Startup) }, Configuration)
                     .WithContainerName(elsaSection.GetSection("Server:ContainerName").Get<string>())
                     .AddWorkflow<TestWorkflow>()
                 );

            _ = services
                .AddElsaSwagger()
                .AddElsaApiEndpoints();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            _ = app.UseWebSockets();
            _ = app.UseCors("CorsPolicy");
            _ = app.UseRouting();
            _ = app.UseEndpoints(e =>
            {
                _ = e.MapHub<SmartHome>("/SmartHome");
            });
        }
    }
}
