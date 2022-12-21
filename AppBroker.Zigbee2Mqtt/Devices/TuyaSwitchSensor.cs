using SocketIOClient;
using AppBroker.Core.Models;
using AppBroker.Core.Devices;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("TS011F_plug_1")]
public class TuyaSwitchSensor : ZigbeeSwitch
{

    public TuyaSwitchSensor(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = true;
    }

    public override async Task<List<History>> GetHistory(DateTimeOffset start, DateTimeOffset end)
    {
        var loadPower = GetHistory(start, end, "load_power");
        var current = GetHistory(start, end, "current");
        var energy = GetHistory(start, end, "energy");
        var voltage = GetHistory(start, end, "voltage");

        var result = new List<History>
            {
                await loadPower,
                await current,
                await energy,
                await voltage,

            };
        return result;
    }
}
