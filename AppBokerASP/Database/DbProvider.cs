using AppBokerASP.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Database
{
    public static class DbProvider
    {
        public static BrokerDbContext BrokerDbContext => new BrokerDbContext();
        static DbProvider() => _ = BrokerDbContext.Database.EnsureCreated();

    }
}
