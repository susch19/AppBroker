using AppBrokerASP.Database.Model;
using AppBrokerASP.Devices;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Database
{
    public class BrokerDbContextOld : DbContext
    {
        public DbSet<HeaterConfigModel> HeaterConfigs { get; set; }
        public DbSet<DeviceModel> Devices { get; set; }
        public DbSet<DeviceMappingModel> DeviceToDeviceMappings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlite("Data Source=broker2.db");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<HeaterConfigModel>().HasNoKey();
        }
    }
}
