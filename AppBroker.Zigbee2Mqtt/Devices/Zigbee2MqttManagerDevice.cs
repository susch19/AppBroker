using AppBroker.Core;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Zigbee2Mqtt.Devices;
public class Zigbee2MqttManagerDevice : Zigbee2MqttDevice
{


    public Zigbee2MqttManagerDevice(Zigbee2MqttDeviceJson device, long id) : base(device, id, nameof(Zigbee2MqttManagerDevice))
    {
        ShowInApp = true;
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        switch (command)
        {
            case (Command)160:
                break;
        }
        return base.UpdateFromApp(command, parameters);
    }
}
