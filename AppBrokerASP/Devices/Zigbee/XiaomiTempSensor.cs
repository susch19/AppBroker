using SocketIOClient;
using System;

namespace AppBrokerASP.Devices.Zigbee
{
    [DeviceName("lumi.weather", "WSDCGQ11LM")]
    public class XiaomiTempSensor : ZigbeeDevice
    {
        public event EventHandler<float>? TemperatureChanged;
        public new bool IsConnected => Available;
        public new bool Available { get; set; }
        public float Temperature
        {
            get => temperature; set
            {
                temperature = value;
                TemperatureChanged?.Invoke(this, value);
                //PrintableInformation[0] = $"Temp: {value.ToString()}";
            }
        }
        public float Humidity { get; set; }
        public float Pressure { get; set; }

        public byte Battery { get; set; }
        public float Voltage { get; set; }

        private float temperature;


        public XiaomiTempSensor(long id, SocketIO socket) : base(id, socket)
        {
            ShowInApp = true;
            Available = true;
        }
    }
}
