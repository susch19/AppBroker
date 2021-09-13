using AppBrokerASP.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Database.Model
{
    public class DeviceModel 
    {
        [Key]
        public long Id { get; set; }
        public string TypeName { get; set; } = "";
        public string FriendlyName { get; set; } = "";
    }
}
