using SocketIOClient;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public abstract class UpdateableZigbeeDevice : ZigbeeDevice
    {
        public UpdateableZigbeeDevice(long nodeId, SocketIO socket) :
            base(nodeId, socket)
        {

        }

        protected Task SetValue(string property, object value)
        {
            return Socket.EmitAsync("setState", $"{AdapterWithId}.{property.ToLower()}", value);
        }
    }
}
