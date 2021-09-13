using AppBrokerASP.Database;
using AppBrokerASP.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbMigrator.V1ToV2.Database
{
    public static class DbProvider
    {
        public static BrokerDbContext BrokerDbContext => new();
        public static BrokerDbContextOld BrokerDbContextOld => new();
        static DbProvider()
        {
            _ = BrokerDbContext.Database.EnsureCreated();
            _ = BrokerDbContextOld.Database.EnsureCreated();
        }
    }
}
