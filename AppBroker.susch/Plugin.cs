
using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;

using NLog;

namespace AppBroker.susch;

public class ByteDevice : Device
{
    public bool? Something
    {
        get => IInstanceContainer.Instance.DeviceStateManager.GetSingleState(Id, "something")?.ToObject<bool>();
        set => IInstanceContainer.Instance.DeviceStateManager.SetSingleState(Id, "something", value);
    }

    public ByteDevice(long nodeId, string? typeName) : base(nodeId, typeName)
    {
    }
}
internal class Plugin : IPlugin
{
    public string Name => "Zigbee2MQTT";
    private readonly List<Newtonsoft.Json.Linq.JToken> emptyParams = new();


    public bool Initialize(LogFactory logFactory)
    {

        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;

        //var add = new GroupingDevice<float>(0xDDFF, GroupingMode.Avg, "temperature", 0x00158d0002c9ff2a, 0x00158d0002c7775e, 0x00158d0002ca01dc, 0x00158d0002ca02fb, 1234);

        //IInstanceContainer.Instance.DeviceManager.AddNewDevice(add);

        //var bd1 = new ByteDevice(0xFF00, "bytedevice");
        //var bd2 = new ByteDevice(0xFF01, "bytedevice");
        //var bd3 = new ByteDevice(0xFF02, "bytedevice");
        //var bd4 = new ByteDevice(0xFF03, "bytedevice");
        //var byteDevices = new[] { bd1, bd2, bd3, bd4 };
        //Task.Run(async () => {
        //    var random = new Random();
        //    await Task.Delay(1000);
        //    while (true)
        //    {
        //        await Task.Delay(300);
        //        var next = random.Next(0, 4);
        //        var bd = byteDevices[next];
        //        bd.Something = !(bd.Something ?? false);
        //    }
        //});

        //IInstanceContainer.Instance.DeviceManager.AddNewDevices(byteDevices);
        //var byteGroup = new GroupingDevice<byte>(0xDEFF, GroupingMode.Min, "something", bd1.Id, bd2.Id, bd3.Id, bd4.Id);

        //IInstanceContainer.Instance.DeviceManager.AddNewDevice(byteGroup);
        return true;
    }

    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if ((ulong)e.Id is 0x7cb03eaa0a0869d9 or 0xa4c1380aa5199538
            && e.PropertyName == "state"
            && e.NewValue.ToObject<bool>() == false
            && IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(e.Id, out Device? device))
        {
            _ = device.UpdateFromApp(Command.On, emptyParams); //Simulate toggling to true via app
        }
        else if ((ulong)e.Id is 0x001788010c255322
            && e.PropertyName == "action")
        {
            var toControlJson = IInstanceContainer.Instance.DeviceStateManager.GetSingleState(e.Id, "controlId");
            long lamp = unchecked((long)0xbc33acfffe180f06), ledstrip = 763955710;


            if (toControlJson is null)
                toControlJson = lamp;

            long controlId = toControlJson.ToObject<long>();

            var currentState = (IInstanceContainer.Instance.DeviceStateManager.GetSingleState(controlId, "state") ?? false).ToObject<bool>();

            var gotDevice = IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(controlId, out var dev);
            switch (e.NewValue.ToObject<HueRemoteAction>())
            {
                case HueRemoteAction.on_press:
                    if (!gotDevice)
                        dev.ReceivedNewState("state", currentState ? "OFF" : "ON", StateFlags.All);

                    break;
                case HueRemoteAction.on_hold:
                    if (controlId != lamp)
                        IInstanceContainer.Instance.DeviceStateManager.SetSingleState(e.Id, "controlId", lamp);
                    break;
                case HueRemoteAction.on_press_release:
                    break;
                case HueRemoteAction.on_hold_release:
                    break;
                case HueRemoteAction.off_press:
                    break;
                case HueRemoteAction.off_hold:
                    if (controlId != ledstrip)
                        IInstanceContainer.Instance.DeviceStateManager.SetSingleState(e.Id, "controlId", ledstrip);
                    break;
                case HueRemoteAction.off_press_release:
                    break;
                case HueRemoteAction.off_hold_release:
                    break;
                case HueRemoteAction.up_press:
                    break;
                case HueRemoteAction.up_hold:
                    break;
                case HueRemoteAction.up_press_release:
                    break;
                case HueRemoteAction.up_hold_release:
                    break;
                case HueRemoteAction.down_press:
                    break;
                case HueRemoteAction.down_hold:
                    break;
                case HueRemoteAction.down_press_release:
                    break;
                case HueRemoteAction.down_hold_release:
                    break;
            }
        }
    }

    internal enum HueRemoteAction
    {
        on_press, on_hold, on_press_release, on_hold_release, off_press, off_hold, off_press_release, off_hold_release, up_press, up_hold, up_press_release, up_hold_release, down_press, down_hold, down_press_release, down_hold_release, recall_0, recall_1
    }
}
