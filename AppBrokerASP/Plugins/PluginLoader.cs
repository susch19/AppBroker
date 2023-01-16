
using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;
using AppBroker.Core.HelperMethods;

using Microsoft.AspNetCore.Mvc;

using NLog;

using System.Reflection;
using System.Runtime.Loader;

using ILogger = NLog.ILogger;

namespace AppBrokerASP.Plugins;

public class PluginLoader
{

    public List<Type> ControllerTypes { get; } = new List<Type>();

    private readonly List<IPlugin> plugins = new();
    private readonly ILogger logger;

    public PluginLoader(LogFactory logFactory)
    {
        logger = logFactory.GetCurrentClassLogger();
    }

    internal void LoadPlugins(Assembly ass)
    {
        Type[] allOfThemTypes = ass.GetTypes();

        foreach (Type type in allOfThemTypes)
        {
            if (!type.IsInterface && !type.IsAbstract)
            {
                if (typeof(IPlugin).IsAssignableFrom(type))
                {
                    logger.Info($"Loading Plugin {type.Name} from Assembly {ass.FullName}");
                    plugins.Add(PluginCreator<IPlugin>.GetInstance(type));
                }
                else if (typeof(Device).IsAssignableFrom(type))
                {
                    IInstanceContainer.Instance.DeviceTypeMetaDataManager.RegisterDeviceType(type);
                }
                else if (typeof(ControllerBase).IsAssignableFrom(type))
                {
                    ControllerTypes.Add(type);
                }
            }
        }
    }

    public void LoadAssemblies()
    {
        string? workdir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (Directory.Exists(Path.Combine(workdir, "plugins")))
        {
            var toRemove = new FileInfo(Path.Combine(workdir, "plugins", "ToRemove.txt"));
            if (toRemove.Exists)
            {
                foreach (string path in File.ReadAllLines(toRemove.FullName))
                {
                    logger.Info($"Deleting existing file {path}");
                    File.Delete(path);
                }
            }

            string[] plugins = Directory.GetFiles(Path.Combine(workdir, "plugins"), "*.dll");
            foreach (string plugin in plugins)
            {
                string filename = Path.GetFileName(plugin);
                logger.Info($"Copying Plugin Assembly {filename}");

                File.Copy(plugin, Path.Combine(workdir, filename), true);
            }

            IEnumerable<string> thirdParty = Directory
                .GetFiles(Path.Combine(workdir, "plugins"))
                .Where(x => !x.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase));

            foreach (string? plugin in thirdParty)
            {
                string filename = Path.GetFileName(plugin);
                logger.Info($"Copying Third Party File {filename}");

                File.Copy(plugin, Path.Combine(workdir, filename), true);
            }
        }

        string[] paths = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll");
        IOrderedEnumerable<(Assembly assembly, PluginAttribute? pluginAttribute)> assemblies = paths
            .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
            .Select(x => (assembly: x, pluginAttribute: x.GetCustomAttribute<PluginAttribute>()))
            .Where(x => x.pluginAttribute is not null)
            .OrderBy(x => x.pluginAttribute!.LoadingPriority);

        foreach ((Assembly assembly, PluginAttribute pluginAttribute) in assemblies)
        {
            logger.Info($"Loading Plugins from Assembly {assembly.FullName} with priority {pluginAttribute!.LoadingPriority}");
            LoadPlugins(assembly);

        }
    }

    public void InitializePlugins(LogFactory logFactory)
    {
        foreach (IPlugin plugin in plugins)
            plugin.RegisterTypes();

        foreach (IPlugin plugin in plugins)
        {
            if (!plugin.Initialize(logFactory))
                logger.Warn($"Plugin {plugin.Name} had errors in initialization :(");
        }
    }
}
