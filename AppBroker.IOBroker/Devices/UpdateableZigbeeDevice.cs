using AppBroker.Core.Javascript;

using Jint;

using NiL.JS.Core;

namespace AppBroker.IOBroker.Devices;

public class UpdateableZigbeeDevice : ZigbeeDevice
{
    public UpdateableZigbeeDevice(long nodeId, SocketIOClient.SocketIO socket, string typeName) :
        base(nodeId, socket, typeName)
    {

    }

    public virtual Task SetValue(string property, object value)
        => Socket.EmitAsync("setState", $"{AdapterWithId}.{property.ToLower()}", value);

    protected override Context ExtendEngine(Context engine)
    {
        engine.DefineFunction("setIoBrokerValue", SetValue);
        return base.ExtendEngine(engine);
    }

    protected override Engine ExtendEngine(Engine engine)
        => base.ExtendEngine(engine
            .SetValue("setIoBrokerValue", SetValue));
}
