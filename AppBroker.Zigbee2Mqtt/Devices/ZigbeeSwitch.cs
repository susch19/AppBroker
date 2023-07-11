using AppBroker.Core;

using Newtonsoft.Json.Linq;


namespace AppBroker.Zigbee2Mqtt.Devices;

public class ZigbeeSwitch : Zigbee2MqttDevice
{

    protected ZigbeeSwitch(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = true;
    }
    protected ZigbeeSwitch(Zigbee2MqttDeviceJson device, long nodeId, string typeName) : base(device, nodeId, typeName)
    {
        ShowInApp = true;
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.On:
            case Command.SingleColor:
                SetState("state", true);
                await zigbeeManager.SetValue(FriendlyName, "state", "ON");
                break;

            case Command.Off:
                SetState("state", false);
                await zigbeeManager.SetValue(FriendlyName, "state", "OFF");
                break;
            case Command.None:
                Console.WriteLine(string.Join(",", parameters.Select(x => x.ToObject<string>())));
                break;
        }
    }
}
