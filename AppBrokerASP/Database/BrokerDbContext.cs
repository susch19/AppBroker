using AppBrokerASP.Database.Model;

using Microsoft.EntityFrameworkCore;

namespace AppBrokerASP.Database;


public class BrokerDbContext : DbContext
{
    public DbSet<HeaterConfigModel> HeaterConfigs => Set<HeaterConfigModel>();
    //public DbSet<HeaterConfigModel> HeaterCalibrations { get; set; }
    public DbSet<DeviceModel> Devices => Set<DeviceModel>();
    public DbSet<DeviceMappingModel> DeviceToDeviceMappings => Set<DeviceMappingModel>();
    //public DbSet<HeaterConfigTemplateModel> HeaterConfigTemplates { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<HeaterConfigModel>().HasNoKey();
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder
        .UseSqlite("Data Source=broker.db")
        .UseLazyLoadingProxies();
}
