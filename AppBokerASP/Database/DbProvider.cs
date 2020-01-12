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


        public static bool AddDeviceToDb(Device d)
        {
            using var cont = BrokerDbContext;
            if (!cont.Devices.Any(x => x.Id == d.Id))
            {
                cont.Add(d.GetModel());
                cont.SaveChanges();
                return true;
            }
            return false;
        }

        public static bool UpdateDeviceInDb(Device d)
        {
            using var cont = BrokerDbContext;
            var dbDevice = cont.Devices.FirstOrDefault(x => x.Id == d.Id);
            if (dbDevice != default)
            {
                dbDevice.FriendlyName = d.FriendlyName;
                cont.SaveChanges();
                return true;
            }
            return false;
        }

        public static bool MergeDeviceWithDbData(Device d)
        {
            using var cont = BrokerDbContext;
            var dbDevice = cont.Devices.FirstOrDefault(x => x.Id == d.Id);
            if (dbDevice != default)
            {
                d.FriendlyName = dbDevice.FriendlyName;
                return true;
            }
            return false;
        }
    }
}
