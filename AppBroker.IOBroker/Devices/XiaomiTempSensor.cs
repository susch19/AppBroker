using AppBroker.Core.Devices;

using SocketIOClient;

namespace AppBroker.IOBroker.Devices;

[DeviceName("lumi.weather", "WSDCGQ11LM")]

public partial class XiaomiTempSensor : ZigbeeDevice
{

    public XiaomiTempSensor(long id, SocketIO socket) : base(id, socket, nameof(XiaomiTempSensor))
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
