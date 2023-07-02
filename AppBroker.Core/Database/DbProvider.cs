using AppBroker.Core.Database.History;
using AppBroker.Core.Devices;

using AppBrokerASP.Devices;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.Globalization;

namespace AppBroker.Core.Database;

public static class DbProvider
{
    public static BrokerDbContext BrokerDbContext => new();

    public static HistoryDbContext HistoryContext => new();

    private class CountResult
    {
        public int Count { get; set; }
    }

    static DbProvider()
    {
        using var ctx = BrokerDbContext;
        using var ctx2 = HistoryContext;

        if (ctx2.Database.CanConnect() && ctx2.DatabaseType.Contains("sqlite", StringComparison.OrdinalIgnoreCase))
        {
            RenameTable(ctx2, "Devices", "HistoryDevices");
            RenameTable(ctx2, "Properties", "HistoryProperties");
            RenameTable(ctx2, "HistoryValueBase", "HistoryValueBases");
            RenameTable(ctx2, "HistoryValueBool", "HistoryValueBools");
            RenameTable(ctx2, "HistoryValueDateTime", "HistoryValueDateTimes");
            RenameTable(ctx2, "HistoryValueDouble", "HistoryValueDoubles");
            RenameTable(ctx2, "HistoryValueHeaterConfig", "HistoryValueHeaterConfigs");
            RenameTable(ctx2, "HistoryValueLong", "HistoryValueLongs");
            RenameTable(ctx2, "HistoryValueString", "HistoryValueStrings");
            RenameTable(ctx2, "HistoryValueTimeSpan", "HistoryValueTimeSpans");

            var maxMigrationId = ctx2.Database.SqlQueryRaw<string>("select MigrationId from __EFMigrationsHistory ORDER BY MigrationId desc LIMIT 1").ToArray().First();
            if (DateTime.TryParseExact(maxMigrationId[..14], "yyyyMMddHHmmss", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                if (dt > new DateTime(2022, 09, 08, 17, 08, 54))
                {
                    ctx2.Database.ExecuteSqlRaw("insert or replace into __EFMigrationsHistory (MigrationId, ProductVersion) values ('20220908170854_AddFields', '6.0.9');");
                }
            }
            ctx2.SaveChanges();
        }

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

    private static void RenameTable(DbContext ctx, string oldName, string newName)
    {

        var res = ctx.Database.SqlQueryRaw<int>($"select count(*) as Count FROM sqlite_master WHERE type='table' and name='{oldName}'").ToArray();
        if (res[0] == 1)
        {
            ctx.Database.ExecuteSqlRaw($"ALTER TABLE {oldName} RENAME TO {newName}");
        }
    }
}
