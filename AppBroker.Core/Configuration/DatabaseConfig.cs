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


    }

}
