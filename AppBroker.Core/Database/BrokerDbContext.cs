using AppBroker.Core.Database.Model;

using Microsoft.EntityFrameworkCore;

namespace AppBroker.Core.Database;


public class BaseDbContext : DbContext
{

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeLongConverter>();
        configurationBuilder
            .Properties<TimeSpan>()
            .HaveConversion<TimeSpanLongConverter>();
    }
}

public class BrokerDbContext : BaseDbContext
{
    public DbSet<HeaterConfigModel> HeaterConfigs => Set<HeaterConfigModel>();
    //public DbSet<HeaterConfigModel> HeaterCalibrations { get; set; }
    public DbSet<DeviceModel> Devices => Set<DeviceModel>();
    public DbSet<DeviceMappingModel> DeviceToDeviceMappings => Set<DeviceMappingModel>();
    //public DbSet<HeaterConfigTemplateModel> HeaterConfigTemplates { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
        .UseSqlite("Data Source=broker.db")
        .UseLazyLoadingProxies();
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
