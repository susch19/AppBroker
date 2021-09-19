﻿using AppBrokerASP.Devices;

using NLog.Fluent;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBrokerASP.Database
{
    public static class DbProvider
    {
        public static BrokerDbContext BrokerDbContext => new();
        static DbProvider() => _ = BrokerDbContext.Database.EnsureCreated();


        public static bool AddDeviceToDb(Device d)
        {
            using var cont = BrokerDbContext;
            if (!cont.Devices.Any(x => x.Id == d.Id))
            {
                _ = cont.Add(d.GetModel());
                _ = cont.SaveChanges();
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
                _ = cont.SaveChanges();
                return true;
            }
            return false;
        }

        public static bool MergeDeviceWithDbData(Device d)
        {
            using var cont = BrokerDbContext;
            try
            {
                var dbDevice = cont.Devices.FirstOrDefault(x => x.Id == d.Id);
                if (dbDevice != default)
                {
                    if (string.IsNullOrWhiteSpace(dbDevice.FriendlyName))
                        d.FriendlyName = dbDevice.Id.ToString();
                    else
                        d.FriendlyName = dbDevice.FriendlyName;
                    return true;
                }
            }
            catch
            {
                d.FriendlyName = "";
                
            }
         
            return false;
        }
    }
}