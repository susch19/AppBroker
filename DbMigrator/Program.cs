
using AppBroker.Core;
using AppBroker.Core.Configuration;
using AppBroker.Core.Database;
using AppBroker.Core.Database.History;

using AppBrokerASP.Configuration;

using Elsa.ActivityResults;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

using NonSucking.Framework.Extension.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using static Org.BouncyCastle.Math.EC.ECCurve;

namespace DbMigrator;


class DbConfig
{

    public string PluginName { get; set; }
    public string ConnectionString { get; set; }
}

class MigratorConfig
{
    public DbConfig SourceBrokerConfig { get; set; }
    public DbConfig TargetBrokerConfig { get; set; }
    public DbConfig SourceHistoryConfig { get; set; }
    public DbConfig TargetHistoryConfig { get; set; }
}

class Program
{
    internal static MigratorConfig config;
    static int counter = 0;
    static int globalCounter = 0;
    static List<object> list = new List<object>();
    static Stopwatch stopwatch = new Stopwatch();
    static Stopwatch stopwatch2 = new Stopwatch();

    const int InsertCount = 1000000;
    static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
            .Build();

        config = new MigratorConfig();
        configuration.GetSection("MigratorConfig").Bind(config);

        if (!string.IsNullOrWhiteSpace(config.SourceHistoryConfig.ConnectionString)
            && !string.IsNullOrWhiteSpace(config.SourceHistoryConfig.PluginName)
            && !string.IsNullOrWhiteSpace(config.TargetHistoryConfig.ConnectionString)
            && !string.IsNullOrWhiteSpace(config.TargetHistoryConfig.PluginName))
        {
            stopwatch2.Restart();
            MigrateHistory();
            Console.WriteLine($"Migrated history in {stopwatch2.Elapsed}");
        }

        if (!string.IsNullOrWhiteSpace(config.SourceBrokerConfig.ConnectionString)
            && !string.IsNullOrWhiteSpace(config.SourceBrokerConfig.PluginName)
            && !string.IsNullOrWhiteSpace(config.TargetBrokerConfig.ConnectionString)
            && !string.IsNullOrWhiteSpace(config.TargetBrokerConfig.PluginName))
        {
            stopwatch2.Restart();
            MigrateBroker();
            Console.WriteLine($"Migrated history in {stopwatch2.Elapsed}");
        }

    }


    private static void MigrateBroker()
    {
        using var source = new BrokerDbContextSource();

        AddRange<AppBroker.Core.Database.Model.DeviceModel>(
            () => new BrokerDbContextTarget(), 
            source.Devices.EntityType, 
            ()=> new BrokerDbContextSource());
        AddRange<AppBroker.Core.Database.Model.HeaterConfigModel>(
            () => new BrokerDbContextTarget(), 
            source.HeaterConfigs.EntityType, 
            () => new BrokerDbContextSource());
        AddRange<AppBroker.Core.Database.Model.DeviceMappingModel>(
            () => new BrokerDbContextTarget(), 
            source.DeviceToDeviceMappings.EntityType, 
            () => new BrokerDbContextSource());
    }
    private static void MigrateHistory()
    {
        using var source = new HistoryContextSource();

        AddRange<HistoryDevice>(()=> new HistoryContextTarget(), source.Devices.EntityType, () => new HistoryContextSource());
        AddRange<HistoryProperty>(() => new HistoryContextTarget(), source.Properties.EntityType, () => new HistoryContextSource());
        AddRange<HistoryValueBase>(() => new HistoryContextTarget(), source.ValueBases.EntityType, () => new HistoryContextSource());
        //AddRange<>(target, source.Devices);
        //AddRange<>(target, source.Properties);
        //AddRange<>(target, source.ValueBases);
    }

    static void AddRange<T>(Func<DbContext> getTarget, IEntityType et, Func<DbContext> getSource) where T : class
    {
        try
        {
            using var src = getSource();

            var tableName = et.GetTableName();


            var eCount = src.Set<T>().Count();

            stopwatch.Restart();
            for (int i = 0; i < eCount; i+= InsertCount)
            {
                using var source = getSource();
                var target = getTarget();
                //using var trans = target.Database.BeginTransaction();
                //target.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} ON");
                /*trans = */
                EntityInsertion(target, source.Set<T>().Skip(i).Take(InsertCount)/*, trans, tableName*/);
                //Console.WriteLine($"Done {i} from {eCount}");
                var saved = i + InsertCount;
                Console.WriteLine($"Saved {(saved < eCount ? saved : eCount)} from {eCount} records in {stopwatch.ElapsedMilliseconds}ms");
                stopwatch.Restart();
                target.SaveChanges();
                //trans.Commit();
            }


            //target.AddRange(list);
            //target.SaveChanges();
            //trans.Commit();
            ////list.Clear();
            //Console.WriteLine($"Saved {counter} records in {stopwatch.ElapsedMilliseconds}ms");
            //Interlocked.Exchange(ref counter, 0);
            //target.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} OFF");
        }
        finally
        {
            //trans.Dispose();
        }
    }

    private static void EntityInsertion<T>(DbContext target, IQueryable<T> entities/*, IDbContextTransaction trans, string tableName*/) where T : class
    {
        //foreach (var entity in entities)
        //{
        //list.Add(entity);
        var c = entities.ToArray();
        target.BulkInsert(c, options => {
            options.InsertKeepIdentity = true;
            options.AllowConcurrency = true;
            }
        );
        var newCounter = Interlocked.Exchange(ref counter, counter + c.Length);

        //if (newCounter >= 100000)
        //{
        //    //target.AddRange(list);
        //    target.SaveChanges();
        //    //trans.Commit();
        //    //trans.Dispose();
        //    //trans = target.Database.BeginTransaction();
        //    //target.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT {tableName} ON");
        //    //list.Clear();
        //    Console.WriteLine($"Saved {newCounter} records in {stopwatch.ElapsedMilliseconds}ms");
        //    stopwatch.Restart();
        //    Interlocked.Exchange(ref counter, 0);
        //}
        //}

        //return trans;
    }
}
