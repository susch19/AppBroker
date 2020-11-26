using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP
{
    public class DeviceService
    {
        public List<Type> DeviceManagers { get; set; }

        public DeviceService(List<Type> deviceManagers)
        {
            DeviceManagers = deviceManagers;
        }




    }
}
