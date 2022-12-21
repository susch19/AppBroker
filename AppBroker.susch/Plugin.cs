

using AppBroker.Core;
using AppBroker.Core.Extension;

using NLog;

namespace AppBroker.susch;

internal class Plugin : IPlugin
{
    public string Name => "Zigbee2MQTT";
    private List<Newtonsoft.Json.Linq.JToken> emptyParams = new();

    public bool Initialize(LogFactory logFactory)
    {

        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;

        return true;
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if ((ulong)e.Id is 0x7cb03eaa0a0869d9 or 0xa4c1380aa5199538
            && e.PropertyName == "state"
            && e.NewValue.ToObject<bool>() == false
            && IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(e.Id, out var device))
        {
            device.UpdateFromApp(Command.On, emptyParams); //Simulate toggling to true via app
        }
    }
}
