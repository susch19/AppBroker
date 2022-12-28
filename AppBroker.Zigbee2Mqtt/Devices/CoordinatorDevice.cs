using AppBroker.Core;

using MQTTnet.Extensions.ManagedClient;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Zigbee2Mqtt.Devices;
public class CoordinatorDevice : Zigbee2MqttDevice
{
    public CoordinatorDevice(Zigbee2MqttDeviceJson device, long id) : base(device, id)
    {
    }

    public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        var cmd = (int)command;


        switch (cmd)
        {
            case 170:
                if (parameters.Count < 1)
                    break;
                var enable = parameters[0].ToObject<bool>();
                int time = 300; //default to 5 mins
                if (parameters.Count > 1)
                    time = parameters[1].ToObject<int>();
                await client.EnqueueAsync("zigbee2mqtt/bridge/request/permit_join", $$"""{"value": {{enable}}, "time": {{time}}}""");
                
                break;
            case 171:
                if (parameters.Count < 1)
                    break;
                //https://www.zigbee2mqtt.io/guide/usage/mqtt_topics_and_messages.html#zigbee2mqtt-bridge-request
                // implement remove, remove force and remove block variants
                await client.EnqueueAsync("zigbee2mqtt/bridge/request/device/remove", $$"""{}""");
                break;
            case 173:
                await client.EnqueueAsync("zigbee2mqtt/bridge/request/restart");
                break;
            case 174:
                await client.EnqueueAsync("zigbee2mqtt/bridge/request/backup");
                break;

            default:
                break;
        }

        await base.UpdateFromApp(command, parameters);
    }
}
