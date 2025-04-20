using AppBroker.Core;
using AppBroker.Zigbee2Mqtt.Math;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Zigbee2Mqtt.Devices;
public class ZigbeeLamp : Zigbee2MqttDevice
{

    private static readonly SteppedFunc polynomial;

    static ZigbeeLamp()
    {
        polynomial = new SteppedFunc((30, 1f), (60, 2f), (90, 3.8f), (100, 5f));
    }

    protected ZigbeeLamp(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = true;
    }
    protected ZigbeeLamp(Zigbee2MqttDeviceJson device, long nodeId, string typeName) : base(device, nodeId, typeName)
    {
        ShowInApp = true;
    }

    public override async void OptionsFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Delay:
                var transitionTime = parameters[0].ToObject<float>();
                SetState(nameof(transitionTime), transitionTime);
                await zigbeeManager.SetValue(Id, "transition_time", transitionTime);
                break;
        }
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case Command.Temp:
                var colorTemp = parameters[0].ToObject<int>();
                SetState(nameof(colorTemp), colorTemp);
                await zigbeeManager.SetValue(Id, "color_temp", colorTemp);
                break;
            case Command.Brightness:
                var brightness = System.Math.Clamp(parameters[0].ToObject<byte>(), (byte)0, (byte)254);
                SetState(nameof(brightness), brightness);
                await zigbeeManager.SetValue(Id, nameof(brightness), brightness);
                break;
            case Command.SingleColor:
                var state = true;
                SetState(nameof(state), state);
                await zigbeeManager.SetValue(Id, nameof(state), "ON");
                break;
            case Command.Off:
                state = false;
                SetState(nameof(state), state);
                await zigbeeManager.SetValue(Id, nameof(state), "OFF");
                break;
            default:
                break;
        }
    }

}
