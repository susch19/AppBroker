using AppBokerASP.Database;
using AppBokerASP.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DbMigrator.V1ToV2.Database
{
    public static class DbProvider
    {
        public static BrokerDbContext BrokerDbContext => new BrokerDbContext();
        public static BrokerDbContextOld BrokerDbContextOld => new BrokerDbContextOld();
        static DbProvider()
        {
            BrokerDbContext.Database.EnsureCreated();
            BrokerDbContextOld.Database.EnsureCreated();
        }
    }
}
