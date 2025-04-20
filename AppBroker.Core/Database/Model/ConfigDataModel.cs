using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database.Model;

[Table("ConfigDatas")]
public class ConfigDataModel
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Key { get; set; }

    public string Value { get; set; }
}
