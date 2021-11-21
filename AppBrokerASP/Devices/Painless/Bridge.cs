namespace AppBrokerASP.Devices.Painless;

public class Bridge : PainlessDevice
{
    public Bridge(long nodeId, List<string> parameter) : base(nodeId)
    {
        ShowInApp = false;
    }
}
