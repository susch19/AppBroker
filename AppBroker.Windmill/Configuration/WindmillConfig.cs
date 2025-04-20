using AppBroker.Core.Configuration;


namespace AppBroker.Windmill.Configuration;

public class WindmillConfig : IConfig
{
    public string Name => "Windmill";

    public string Url { get; set; } = "http://192.168.49.123:8100/api/r/stateChanged";
}
