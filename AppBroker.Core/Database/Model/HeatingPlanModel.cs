using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppBroker.Core.Database.Model;

public class HeatingPlanModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }

    //[InverseProperty(nameof(HeaterConfigModel.HeatingPlan))]
    //public virtual ICollection<HeaterConfigModel>? HeaterConfigs { get; set; }
}