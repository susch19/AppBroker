using AppBroker.Core.Database;

using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigrator;


class BrokerDbContextSource : BrokerDbContext
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseFactory.Initialize(Program.config.SourceBrokerConfig.PluginName);
        DatabaseType = Program.config.SourceBrokerConfig.PluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, Program.config.SourceBrokerConfig.ConnectionString).UseLazyLoadingProxies(false);
        }
    }
}
class BrokerDbContextTarget : BrokerDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseFactory.Initialize(Program.config.TargetBrokerConfig.PluginName);
        DatabaseType = Program.config.TargetBrokerConfig.PluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, Program.config.TargetBrokerConfig.ConnectionString).UseLazyLoadingProxies(false);

        }
    }
}
