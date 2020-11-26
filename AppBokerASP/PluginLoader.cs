using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;
using AppBroker.Core.Helper;

using NLog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace AppBokerASP
{
    public class PluginLoader
    {
        public List<Type> ControllerTypes { get; } = new List<Type>();
        public List<Type> DeviceManagerTypes { get; } = new List<Type>();

        private List<IPlugin> plugins = new List<IPlugin>();
        private ILogger logger = LogManager.GetLogger(nameof(PluginLoader));
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
                else if (typeof(IDeviceManager).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    DeviceManagerTypes.Add(type);
                }

                //else if (typeof(BaseController).IsAssignableFrom(type))
                //{
                //    ControllerTypes.Add(type);
                //}
              
            }
        }

       

        public void LoadAssemblies()
        {
            var workdir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (Directory.Exists(Path.Combine(workdir, "plugins")))
            {
                var plugins = Directory.GetFiles(Path.Combine(workdir, "plugins"), "*.dll");
                foreach (var plugin in plugins)
                {
                    var filename = Path.GetFileName(plugin);
                    logger.Info($"Copying Plugin Assembly {filename}");

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
