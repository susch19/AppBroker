using AppBroker.Core.Database.Model;

using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database;
public class AppDbContext : BaseDbContext
{
    public DbSet<AppModel> Apps => Set<AppModel>();
    public DbSet<AppConfigModel> AppConfigs => Set<AppConfigModel>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbConfig = IInstanceContainer.Instance.ConfigManager.DatabaseConfig;
        DatabaseFactory.Initialize(new FileInfo(dbConfig.BrokerDatabasePluginName).FullName);
        DatabaseType = dbConfig.BrokerDatabasePluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, dbConfig.BrokerDBConnectionString).UseLazyLoadingProxies();
        }

        base.OnConfiguring(optionsBuilder);
    }
}
