using AppBroker.Core.Devices;
using AppBroker.Zigbee2Mqtt;

using SocketIOClient;

namespace AppBroker.Zigbee2Mqtt.Devices;

[DeviceName("lumi.weather", "WSDCGQ11LM")]

public partial class XiaomiTempSensor : Zigbee2MqttDevice
{

    public XiaomiTempSensor(Zigbee2MqttDeviceJson device, long nodeId) : base(device, nodeId)
    {
        ShowInApp = true;
    }

    //public override async Task UpdateFromApp(Command command, List<JToken> parameters)
    //{
    //    switch (command)
    //    {
    //        case Command.None:
    //            var par = parameters.FirstOrDefault();
    //            if (par is null)
    //                return;
    //            if (par.Type == JTokenType.Boolean)
    //            {
    //                overridenState = par.ToObject<bool>();
    //                SendDataToAllSubscribers();
    //            }
    //            break;
    //    }
    //}
}
