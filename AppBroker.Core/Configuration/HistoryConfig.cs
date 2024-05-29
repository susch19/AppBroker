namespace AppBroker.Core.Configuration;


public class HistoryConfig : IConfig
{
    public const string ConfigName = nameof(HistoryConfig);
    public string Name => ConfigName;

    public bool UseOwnHistoryManager { get; set; }

}
