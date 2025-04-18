namespace AppBroker.Core;

public class Subscriber
{
    public string ConnectionId { get; set; }
    public dynamic SmarthomeClient { get; internal set; }
    public Subscriber(string connectionId, dynamic smarthomeClient)
    {
        ConnectionId = connectionId;
        SmarthomeClient = smarthomeClient;
    }
}
