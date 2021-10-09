using System.ComponentModel.DataAnnotations;

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
