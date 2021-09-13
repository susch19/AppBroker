using AppBrokerASP.Configuration;

using Microsoft.Extensions.Configuration;
using PainlessMesh.Ota;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP
{
    public static class InstanceContainer
    {
        public static DeviceManager DeviceManager { get; private set; }

        public static SmarthomeMeshManager MeshManager { get; private set; }

        public static UpdateManager UpdateManager { get; private set; }

        public static ConfigManager ConfigManager { get; private set; }

        static InstanceContainer()
        {


            ConfigManager = new ConfigManager();
            UpdateManager = new();
            MeshManager = new SmarthomeMeshManager(ConfigManager.PainlessMeshConfig.ListenPort);
            DeviceManager = new DeviceManager();
        }

    }
}
