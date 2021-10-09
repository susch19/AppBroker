using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AppBrokerASP
{
    public class Startup
    {
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
