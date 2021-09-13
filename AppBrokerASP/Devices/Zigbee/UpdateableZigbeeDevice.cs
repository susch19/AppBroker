using SocketIOClient;
using System;
using System.Net;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public abstract class UpdateableZigbeeDevice : ZigbeeDevice
    {
        private readonly string baseUpdateUrl;
        public UpdateableZigbeeDevice(long nodeId, string baseUpdateUrl, Type type, SocketIO socket) : base(nodeId, type, socket)
        {
            this.baseUpdateUrl = baseUpdateUrl;
        }
        protected WebResponse UpdateZigbeeDeviceRequest<T>(string valuename, T value)
        {
            var request = WebRequest.CreateHttp(string.Format("{0}.{1}?value={2}", baseUpdateUrl, valuename, value));
            return request.GetResponse();
        }

        protected Task SetValue(string property, object value)
        {
            return Socket.EmitAsync("setState", $"{AdapterWithId}.{property.ToLower()}", value);
        }
    }
}
