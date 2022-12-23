

using AppBroker.Core;
using AppBroker.Core.Extension;
using AppBroker.susch.Devices;

using NLog;

namespace AppBroker.susch;

internal class Plugin : IPlugin
{
    public string Name => "Zigbee2MQTT";
    private List<Newtonsoft.Json.Linq.JToken> emptyParams = new();

    public bool Initialize(LogFactory logFactory)
    {

        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;

        var add = new GroupingDevice<float>(0xDDFF, GroupingMode.Avg, "temperature", 0x00158d0002c9ff2a, 0x00158d0002c7775e, 0x00158d0002ca01dc, 0x00158d0002ca02fb, 1234);

        IInstanceContainer.Instance.DeviceManager.AddNewDevice(add);

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
