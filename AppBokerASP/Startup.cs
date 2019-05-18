using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AppBokerASP.Devices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace AppBokerASP
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSignalR(config);
            app.UseWebSockets();
            app.UseMvc();

            app.Use(async (context, next) =>
            {
                //if (context.Request.Path == "/Heater")
                //{
                //    if (context.WebSockets.IsWebSocketRequest)
                //    {
                //        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                //        var heater = new Heater(webSocket) { Id = DeviceManager.NextId };
                //        DeviceManager.Devices.Add(heater.Id, heater);
                //        await heater.Update(context, webSocket);
                //    }
                //    else
                //    {
                //        context.Response.StatusCode = 400;
                //    }
                //}
                //else
                //{
                //    await next();
                //}
            });
        }

 
        private void config(HubRouteBuilder obj)
        {
            obj.MapHub<SmartHome>(new PathString("/SmartHome"));
            
        }

    }
}
