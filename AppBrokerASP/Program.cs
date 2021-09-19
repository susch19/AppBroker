using System;
using System.Text;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AppBrokerASP
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (InstanceContainer.ConfigManager.PainlessMeshConfig.Enabled)
                InstanceContainer.MeshManager.Start();

            if (args.Length > 0)
            {
                CreateWebHostBuilder(args).UseUrls(args).Build().Run();
            }
            else
            {
#if DEBUG
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://0.0.0.0:5056").Build().Run();
#else
                CreateWebHostBuilder(args).UseUrls("http://[::1]:5056", "http://0.0.0.0:5056").Build().Run();
#endif
            }

            InstanceContainer.Dispose();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
            .CreateDefaultBuilder(args)
            .UseStartup<Startup>();
    }
}
