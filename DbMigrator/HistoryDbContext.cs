using AppBroker.Core.Database.History;
using Microsoft.EntityFrameworkCore;
using NonSucking.Framework.Extension.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbMigrator;


class HistoryContextSource : HistoryDbContext
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseFactory.Initialize(Program.config.SourceHistoryConfig.PluginName);
        DatabaseType = Program.config.SourceHistoryConfig.PluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, Program.config.SourceHistoryConfig.ConnectionString).UseLazyLoadingProxies(false);
        }
    }
}
class HistoryContextTarget : HistoryDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DatabaseFactory.Initialize(Program.config.TargetHistoryConfig.PluginName);
        DatabaseType = Program.config.TargetHistoryConfig.PluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, Program.config.TargetHistoryConfig.ConnectionString).UseLazyLoadingProxies(false);

        }
    }
}
