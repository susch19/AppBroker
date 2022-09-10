using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;

using Elsa.Activities.StateMachine;

using Newtonsoft.Json.Linq;

using SocketIOClient;

using System.Runtime.CompilerServices;

namespace AppBrokerASP.Devices.Zigbee;

[DeviceName("lumi.weather", "WSDCGQ11LM")]

public partial class XiaomiTempSensor : ZigbeeDevice
{

    public XiaomiTempSensor(long id, SocketIO socket) : base(id, socket, nameof(XiaomiTempSensor))
    {
        ShowInApp = true;
        Available = true;
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
