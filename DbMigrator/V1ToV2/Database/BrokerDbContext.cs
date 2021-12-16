using DbMigrator.V1ToV2.Database.Model;

using Microsoft.EntityFrameworkCore;

namespace DbMigrator.V1ToV2.Database;

public class BrokerDbContext : DbContext
{
    public DbSet<HeaterConfigModel> HeaterConfigs { get; set; }
    //public DbSet<HeaterConfigModel> HeaterCalibrations { get; set; }
    public DbSet<DeviceModel> Devices { get; set; }
    public DbSet<DeviceMappingModel> DeviceToDeviceMappings { get; set; }
    public DbSet<HeaterConfigTemplateModel> HeaterConfigTemplates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder.UseSqlite("Data Source=broker.db");
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.Entity<HeaterConfigModel>().HasNoKey();
    }
}
