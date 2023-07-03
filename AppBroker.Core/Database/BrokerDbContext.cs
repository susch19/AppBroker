using AppBroker.Core.Database.Model;

using Microsoft.EntityFrameworkCore;

using NonSucking.Framework.Extension.EntityFrameworkCore;

namespace AppBroker.Core.Database;


public class BaseDbContext : DatabaseContext
{
    public string DatabaseType { get; protected set; }

    public BaseDbContext()
    {
        EnableUseLazyLoading = true;
        AssemblyRootName = nameof(AppBroker);
        AddAllEntities = false;
    }

}

public class BrokerDbContext : BaseDbContext
{
    public DbSet<HeaterConfigModel> HeaterConfigs => Set<HeaterConfigModel>();
    //public DbSet<HeaterConfigModel> HeaterCalibrations { get; set; }
    public DbSet<DeviceModel> Devices => Set<DeviceModel>();
    public DbSet<DeviceMappingModel> DeviceToDeviceMappings => Set<DeviceMappingModel>();
    //public DbSet<HeaterConfigTemplateModel> HeaterConfigTemplates { get; set; }


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

/*
 * Migration Steps:
 * Exectue SQL in db before executin application: 
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);

Insert into "__EFMigrationsHistory" SELECT "20220923182559_Initial", "6.0.7" WHERE NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE MigrationId = "20220923182559_Initial");
 
 */
