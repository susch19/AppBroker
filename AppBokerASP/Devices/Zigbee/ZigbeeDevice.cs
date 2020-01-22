using AppBokerASP.IOBroker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class ZigbeeDevice : Device
    {
        public DateTime LastReceived { get; set; }
        [JsonPropertyName("linkQuality")]
        public byte Link_Quality { get; set; }
        public bool Available { get; set; }

        private readonly ReadOnlyCollection<PropertyInfo> propertyInfos;

        public ZigbeeDevice(ulong nodeId, Type type) : base(nodeId)
        {
            propertyInfos = 
                Array.AsReadOnly(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Concat(typeof(ZigbeeDevice).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .ToArray());
        }

        public void SetPropFromIoBroker(IoBrokerObject ioBrokerObject, bool setLastReceived)
        {
            var prop = propertyInfos.FirstOrDefault(x => x.Name.ToLower() == ioBrokerObject.ValueName);
            if (prop == default || ioBrokerObject.ValueParameter.Value == null)
                return;
            prop.SetValue(this, Convert.ChangeType(ioBrokerObject.ValueParameter.Value, prop.PropertyType));
            if (setLastReceived)
                LastReceived = DateTime.Now;
            SendDataToAllSubscribers();
        }


    }
}
