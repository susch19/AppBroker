using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Database.History;
public class HistoryContext : BaseDbContext
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
        _ = optionsBuilder
            .UseSqlite(IInstanceContainer.Instance.ConfigManager.DatabaseConfig.HistoryDBConnectionString)
            .UseLazyLoadingProxies();
    }
}
