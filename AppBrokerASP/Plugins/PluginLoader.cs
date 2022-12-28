

using NLog;

using AppBroker.Core.HelperMethods;
using AppBroker.Core.Extension;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using ILogger = NLog.ILogger;
using AppBroker.Core.Devices;
using AppBroker.Core;

namespace AppBrokerASP.Plugins
{
    public class PluginLoader
    {

        public List<Type> ControllerTypes { get; } = new List<Type>();

        private List<IPlugin> plugins = new();
        private readonly ILogger logger;

        public PluginLoader(LogFactory logFactory)
        {
            logger = logFactory.GetCurrentClassLogger();
        }

        internal void LoadPlugins(Assembly ass)
        {
            var allOfThemTypes = ass.GetTypes();

            foreach (var type in allOfThemTypes)
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    logger.Info($"Loading Plugin {type.Name} from Assembly {ass.FullName}");
                    plugins.Add(PluginCreator<IPlugin>.GetInstance(type));
                }
                else if (typeof(Device).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    IInstanceContainer.Instance.DeviceTypeMetaDataManager.RegisterDeviceType(type);
                }
            }
        }

        public void LoadAssemblies()
        {
            var workdir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (Directory.Exists(Path.Combine(workdir, "plugins")))
            {
                var toRemove = new FileInfo(Path.Combine(workdir, "plugins", "ToRemove.txt"));
                if (toRemove.Exists)
                {
                    foreach (var path in File.ReadAllLines(toRemove.FullName))
                    {
                        logger.Info($"Deleting existing file {path}");
                        File.Delete(path);
                    }
                }

                var plugins = Directory.GetFiles(Path.Combine(workdir, "plugins"), "*.dll");
                foreach (var plugin in plugins)
                {
                    var filename = Path.GetFileName(plugin);
                    logger.Info($"Copying Plugin Assembly {filename}");

                    File.Copy(plugin, Path.Combine(workdir, filename), true);
                }

                var thirdParty = Directory
                    .GetFiles(Path.Combine(workdir, "plugins"))
                    .Where(x => !x.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase));

                foreach (var plugin in thirdParty)
                {
                    var filename = Path.GetFileName(plugin);
                    logger.Info($"Copying Third Party File {filename}");

                    File.Copy(plugin, Path.Combine(workdir, filename), true);
                }
            }


            var paths = Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll");

            foreach (var path in paths)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                if (assembly.GetCustomAttribute<PluginAttribute>() is not null)
                {
                    logger.Info($"Loading Plugins from Assembly {assembly.FullName}");
                    LoadPlugins(assembly);
                }
            }
        }

        public void InitializePlugins(LogFactory logFactory)
        {
            foreach (var plugin in plugins)
            {
                if (!plugin.Initialize(logFactory))
                    logger.Warn($"Plugin {plugin.Name} had errors in initialization :(");
            }

        }
    }
}
