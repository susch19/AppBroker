using SocketIOClient;

namespace AppBrokerASP.Devices.Zigbee
{
    [DeviceName("lumi.weather", "WSDCGQ11LM")]
    [AppBroker.ClassPropertyChangedAppbroker]
    public partial class XiaomiTempSensor : ZigbeeDevice
    {
        public event EventHandler<float>? TemperatureChanged;
        public new bool IsConnected => Available;
        private float humidity;
        private float pressure;
        private byte battery;
        private float voltage;
        private float temperature;


        public XiaomiTempSensor(long id, SocketIO socket) : base(id, socket)
        {
            ShowInApp = true;
            Available = true;
        }
    }
}
