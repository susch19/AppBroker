using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database.Model;
[Table("Apps")]
public class AppModel
{
    [Key]
    public Guid Id {  get; set; }
    public string Name { get; set; }

    [InverseProperty(nameof(AppConfigModel.App))]
    public ICollection<AppConfigModel> Configs { get; set; }
}
