using AppBroker.Core.Database.History;
using AppBroker.Core.Devices;

using AppBrokerASP.Devices;

using Microsoft.EntityFrameworkCore;


namespace AppBroker.Core.Database;

public static class DbProvider
{
    public static BrokerDbContext BrokerDbContext => new();

    public static HistoryContext HistoryContext => new();
    static DbProvider()
    {

        using var ctx = BrokerDbContext;
        using var ctx2 = HistoryContext;

        ctx.Database.Migrate();
        ctx2.Database.Migrate();

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
            dbDevice.FriendlyUniqueName = d.FriendlyUniqueName;

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
                d.FriendlyUniqueName = string.IsNullOrWhiteSpace(dbDevice.FriendlyUniqueName) ? dbDevice.Id.ToString() : dbDevice.FriendlyUniqueName;
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
