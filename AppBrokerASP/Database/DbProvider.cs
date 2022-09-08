using AppBroker.Core.Devices;

using AppBrokerASP.Devices;

using Microsoft.EntityFrameworkCore;

namespace AppBrokerASP.Database;

public static class DbProvider
{
    public static BrokerDbContext BrokerDbContext => new();
    static DbProvider()
    {
        using var ctx = BrokerDbContext;
        _ = ctx.Database.EnsureCreated();
    }

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
                d.FriendlyName = string.IsNullOrWhiteSpace(dbDevice.FriendlyName) ? dbDevice.Id.ToString() : dbDevice.FriendlyName;
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
