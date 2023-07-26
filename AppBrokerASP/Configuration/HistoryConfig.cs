namespace AppBrokerASP.Configuration;


public enum InMemoryHoldRange 
{
    Unset,
    None,
    Records, 
    Minutes,
    Hours, 
    Days
}
public class HistoryConfig
{
    public const string ConfigName = nameof(HistoryConfig);

    public bool UseOwnHistoryManager { get; set; }
    public InMemoryHoldRange InMemoryHoldRange { get; set; } = InMemoryHoldRange.None;
    public int InMemoryHoldValue { get; set; }
}
