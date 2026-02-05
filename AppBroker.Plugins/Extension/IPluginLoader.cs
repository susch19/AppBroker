using NLog;

namespace AppBroker.Plugins.Extension;

public interface IPluginLoader
{
    public static IPluginLoader Instance { get; set; } = null!;
    List<IAppConfigurator> AppConfigurators { get; }
    List<IConfig> Configs { get; }
    List<Type> ControllerTypes { get; }
    List<Type> DeviceTypes { get; }
    List<IPlugin> Plugins { get; }

    void InitializePlugins(LogFactory logFactory);
    void LoadAssemblies();
}