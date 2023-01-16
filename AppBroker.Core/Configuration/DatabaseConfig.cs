namespace AppBroker.Core.Configuration;


public class DatabaseConfig
{
    public const string ConfigName = nameof(DatabaseConfig);

    public string BrokerDBConnectionString { get; set; }
    public string HistoryDBConnectionString { get; set; }

    public DatabaseConfig()
    {
        BrokerDBConnectionString = "Data Source=broker.db";
        HistoryDBConnectionString = "Data Source=history.db";

    }

}
