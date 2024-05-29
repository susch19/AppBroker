namespace AppBroker.Core.Configuration;


public class DatabaseConfig : IConfig
{
    public const string ConfigName = nameof(DatabaseConfig);
    public string Name => ConfigName;

    public string BrokerDBConnectionString { get; set; }
    public string BrokerDatabasePluginName { get; set; }
    public string HistoryDBConnectionString { get; set; }
    public string HistoryDatabasePluginName { get; set; }

    public DatabaseConfig()
    {
        BrokerDBConnectionString = "Data Source=broker.db";
        HistoryDBConnectionString = "Data Source=history.db";
        BrokerDatabasePluginName = "NonSucking.Framework.Extension.Database.Sqlite.dll";
        HistoryDatabasePluginName = "NonSucking.Framework.Extension.Database.Sqlite.dll";

        //BrokerDBConnectionString = "Data Source=DESKTOP-290LU50\\SQLEXPRESS;Initial Catalog=BrokerDb;Integrated Security=True;Trust Server Certificate=True";
        //HistoryDBConnectionString = "Data Source=DESKTOP-290LU50\\SQLEXPRESS;Initial Catalog=BrokerDb;Integrated Security=True;Trust Server Certificate=True";
        //BrokerDatabasePluginName = "NonSucking.Framework.Extension.Database.MSSQL.dll";
        //HistoryDatabasePluginName = BrokerDatabasePluginName;

        //BrokerDBConnectionString = "Host=localhost;Database=brokerdb;Username=postgres;Password=mysecretpassword";
        //BrokerDBConnectionString = "Host=192.168.49.56;Database=BrokerDb;Username=postgres;Password=mysecretpassword";
        //BrokerDBConnectionString = "Host=192.168.49.29;Database=BrokerDb;Username=susch19;Password=126978453";
        //HistoryDBConnectionString = BrokerDBConnectionString;
        //BrokerDatabasePluginName = @"C:\Users\susch\source\repos\AppBroker\AppBrokerASP\bin\Debug\net8.0\NonSucking.Framework.Extension.Database.PostgreSQL.dll";
        //HistoryDatabasePluginName = BrokerDatabasePluginName;


        //BrokerDBConnectionString = "Data Source=DESKTOP-290LU50\\SQLEXPRESS;Initial Catalog=HistoryDb;Integrated Security=True;Trust Server Certificate=True";
        //HistoryDBConnectionString = "Data Source=DESKTOP-290LU50\\SQLEXPRESS;Initial Catalog=HistoryDb;Integrated Security=True;Trust Server Certificate=True";
        //BrokerDatabasePluginName = "NonSucking.Framework.Extension.Database.MSSQL.dll";
        //HistoryDatabasePluginName = BrokerDatabasePluginName;

    }

}
