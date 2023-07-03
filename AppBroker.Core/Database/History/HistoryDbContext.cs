using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database.History;
public class HistoryDbContext : BaseDbContext
{
    public DbSet<HistoryDevice> Devices { get; set; }
    public DbSet<HistoryProperty> Properties { get; set; }
    public DbSet<HistoryValueBase> ValueBases { get; set; }

    public DbSet<HistoryValueBool> HistoryBools { get; set; }
    public DbSet<HistoryValueString> HistoryStrings { get; set; }
    public DbSet<HistoryValueDouble> HistoryDoubles { get; set; }
    public DbSet<HistoryValueLong> HistoryLongs { get; set; }
    public DbSet<HistoryValueDateTime> HistoryDates { get; set; }
    public DbSet<HistoryValueTimeSpan> HistoryTimespans { get; set; }
    public DbSet<HistoryValueHeaterConfig> HistoryHeaterConfigs { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbConfig = IInstanceContainer.Instance.ConfigManager.DatabaseConfig;
        DatabaseFactory.Initialize(new FileInfo(dbConfig.HistoryDatabasePluginName).FullName);
        DatabaseType = dbConfig.HistoryDatabasePluginName;
        foreach (var item in DatabaseFactory.DatabaseConfigurators)
        {
            if (DatabaseType.Contains(item.Name, StringComparison.OrdinalIgnoreCase))
                item.OnConfiguring(optionsBuilder, dbConfig.HistoryDBConnectionString).UseLazyLoadingProxies();
        }

        base.OnConfiguring(optionsBuilder);
    }
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (DatabaseType.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            configurationBuilder
                .Properties<DateTime>()
                .HaveConversion<DateTimeLongConverter>();
            configurationBuilder
                .Properties<TimeSpan>()
                .HaveConversion<TimeSpanLongConverter>();
        }
    }
}
