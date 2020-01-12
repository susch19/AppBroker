using AppBokerASP.IOBroker;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AppBokerASP.Devices
{
    public class XiaomiTempSensor : Device
    {
        public event EventHandler<float> TemperatureChanged;
        public new bool IsConnected => Available;
        public bool Available
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
        public byte Link_quality { get; set; }
        public byte Battery
        {
            get => battery; set
            {
                battery = value;
                //PrintableInformation[3] = $"Battery: {value.ToString()}";
            }
        }
        public float Voltage { get; set; }

        private readonly ReadOnlyCollection<PropertyInfo> propertyInfos;
        private float temperature;
        private float humidity;
        private bool available;
        private float pressure;
        private byte battery;

        public XiaomiTempSensor() : base(0)
        {

        }

        public XiaomiTempSensor(ulong id) : base(id)
        {
            var props = typeof(XiaomiTempSensor).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            propertyInfos = Array.AsReadOnly(props);

            ShowInApp = true;
            Available = true;
        }

        public void SetPropFromIoBroker(IoBrokerZigbee zig)
        {
            var prop = propertyInfos.FirstOrDefault(x => x.Name.ToLower() == zig.ValueName);
            if (prop == default || zig.ValueParameter.Value == null)
                return;
            prop.SetValue(this, Convert.ChangeType(zig.ValueParameter.Value, prop.PropertyType));
            SendDataToAllSubscribers();
        }

    }
}
