using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database.Model;

[Table("AppConfigs")]
public class AppConfigModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column(Order = 0)]
    public string Key {  get; set; }

    public string Value { get; set; }

    [ForeignKey("AppId"), Key, Column(Order = 1)]
    public AppModel App { get; set; }
}
