using NLog;
using System.Text;
using AppBrokerASP.Plugins;

namespace AppBrokerASP;

public class Program
{

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var pluginLoader = new PluginLoader(LogManager.LogFactory);
        pluginLoader.LoadAssemblies();
        var runtime = pluginLoader.Plugins.OfType<IRuntimePlugin>().First();
        runtime.InitializeStartup(args);
        pluginLoader.InitializePlugins(LogManager.LogFactory);
        runtime.Run();
    }


}
