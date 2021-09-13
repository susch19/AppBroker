using AppBrokerASP.IOBroker;
using SocketIOClient;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AppBrokerASP.Devices.Zigbee
{
    public class XiaomiTempSensor : ZigbeeDevice
    {
        public event EventHandler<float>? TemperatureChanged;
        public new bool IsConnected => Available;
        public new bool Available
        {
            get => available; set
            {
                available = value;
                //PrintableInformation[4] = $"Available: {value.ToString()}";
            }
        }
        public float Temperature
        {
            get => temperature; set
            {
                temperature = value;
                TemperatureChanged?.Invoke(this, value);
                //PrintableInformation[0] = $"Temp: {value.ToString()}";
            }
        }
        public float Humidity
        {
            get => humidity; set
            {
                humidity = value;
                //PrintableInformation[1] = $"Humidity: {value.ToString()}";
            }
        }
        public float Pressure
        {
            get => pressure; set
            {
                pressure = value;
                //PrintableInformation[2] = $"Pressure: {value.ToString()}";
            }
        }

        public byte Battery
        {
            get => battery; set
            {
                battery = value;
                //PrintableInformation[3] = $"Battery: {value.ToString()}";
            }
        }
        public float Voltage { get; set; }

        private float temperature;
        private float humidity;
        private bool available;
        private float pressure;
        private byte battery;


        public XiaomiTempSensor(long id, SocketIO socket) : base(id, typeof(XiaomiTempSensor), socket)
        {
            ShowInApp = true;
            Available = true;
        }
    }
}
