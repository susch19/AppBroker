using AppBokerASP.IOBroker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static AppBokerASP.IOBroker.IoBrokerHistory;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class ZigbeeDevice : Device
    {
        public DateTime LastReceived { get; set; }
        [JsonPropertyName("linkQuality")]
        public byte Link_Quality { get; set; }
        public bool Available { get; set; } 
        public string AdapterWithId { get; set; } = "";

        private readonly ReadOnlyCollection<PropertyInfo> propertyInfos;

        public ZigbeeDevice(long nodeId, Type type) : base(nodeId)
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
            if ((prop.PropertyType == typeof(float) || prop.PropertyType == typeof(double)) && ioBrokerObject.ValueParameter.Value.ToObject<string>()!.Contains(","))
            {
                var strValue = ioBrokerObject.ValueParameter.Value.ToObject<string>();
                if (double.TryParse(strValue, out var val))
                    prop.SetValue(this, Convert.ChangeType(val, prop.PropertyType));
            }
            else
                prop.SetValue(this, Convert.ChangeType(ioBrokerObject.ValueParameter.Value.ToObject(prop.PropertyType), prop.PropertyType));
            if (setLastReceived)
                LastReceived = DateTime.Now;
            SendDataToAllSubscribers();
        }
        public virtual List<IoBrokerHistory> ReadHistoryJSON(DateTime date)
        {
#if DEBUG
            var files = Directory.GetFiles($"F:\\Temp\\Neuer Ordner\\{date:yyyyMMdd}\\", $"history.{AdapterWithId}*");
#else
            var files = Directory.GetFiles($"/home/pi/IoBrokerHistory/{date.ToString("yyyyMMdd")}/", $"history.{AdapterWithId}*");
#endif

            var list = new List<IoBrokerHistory>();
            foreach (var file in files)
            {
                var hist = Newtonsoft.Json.JsonConvert.DeserializeObject<HistoryRecord[]>(File.ReadAllText(file))!;

                list.Add(new IoBrokerHistory(hist, file!.Split('.').SkipLast(1).Last()));
            }
            return list;
            //return Newtonsoft.Json.JsonConvert.DeserializeObject<IoBrokerHistory>(File.ReadAllText($"/home/pi/ioBrokerHistory/{date.ToString("yyyyMMdd")}/history.{AdapterWithId}.{property}.json"));
        }

        public virtual IoBrokerHistory ReadHistoryJSON(DateTime date, string property)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<IoBrokerHistory>(File.ReadAllText($"/home/pi/ioBrokerHistory/{date:yyyyMMdd}/history.{AdapterWithId}.{property}.json"))!;
        }


    }
}
