using System.ComponentModel.DataAnnotations;

namespace DbMigrator.V1ToV2.Database.Model
{
    public class DeviceModel 
    {
        [Key]
        public long Id { get; set; }
        public string TypeName { get; set; }
        public string FriendlyName { get; set; }
    }
}
