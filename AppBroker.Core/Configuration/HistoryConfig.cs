namespace AppBroker.Core.Configuration;


public class HistoryConfig
{
    public const string ConfigName = nameof(HistoryConfig);

    public bool UseOwnHistoryManager { get; set; }

}
