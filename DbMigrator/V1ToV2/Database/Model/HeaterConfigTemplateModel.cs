using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbMigrator.V1ToV2.Database.Model
{
    public class HeaterConfigTemplateModel
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string TemplateName { get; set; }

        [ForeignKey("DeviceId")]
        public virtual DeviceModel Device { get; set; }
    }
}
