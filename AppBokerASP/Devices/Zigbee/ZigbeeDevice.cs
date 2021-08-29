﻿using AppBokerASP.Configuration;
using AppBokerASP.IOBroker;

using SocketIOClient;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using static AppBokerASP.IOBroker.IoBrokerHistory;

namespace AppBokerASP.Devices.Zigbee
{
    public abstract class ZigbeeDevice : Device
    {
        protected readonly SocketIO Socket;
        public DateTime LastReceived { get; set; }
        [JsonPropertyName("linkQuality")]
        public byte Link_Quality { get; set; }
        public bool Available { get; set; }
        public string AdapterWithId { get; set; } = "";

        private readonly ReadOnlyCollection<PropertyInfo> propertyInfos;

        public ZigbeeDevice(long nodeId, Type type, SocketIO socket) : base(nodeId)
        {
            Socket = socket;
            propertyInfos =
                Array.AsReadOnly(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Concat(typeof(ZigbeeDevice).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .ToArray());
        }

        public void SetPropFromIoBroker(IoBrokerObject ioBrokerObject, bool setLastReceived)
        {
            var prop = propertyInfos.FirstOrDefault(x => ioBrokerObject.ValueName.Contains(x.Name.ToLower()));
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

        public async Task<List<IoBrokerHistory>> GetHistory(DateTimeOffset start, DateTimeOffset end)
        {
            var temp = GetHistory(start, end, HistoryType.Temperature);
            var humidity = GetHistory(start, end, HistoryType.Humidity);
            var pressure = GetHistory(start, end, HistoryType.Pressure);

            var result = new List<IoBrokerHistory>
            {
                await temp,
                await humidity,
                await pressure
            };
            return result;
        }

        public async Task<IoBrokerHistory> GetHistory(DateTimeOffset start, DateTimeOffset end, HistoryType type)
        {
            var history = new IoBrokerHistory(type.ToString().ToLower());

            var readFromFile = ReadHistoryJSON(start.Date, history);
            if (readFromFile is not null)
                return readFromFile;

            history.HistoryRecords = await GetHistoryRecords(start, end, type);
            return history;
        }



        public virtual IoBrokerHistory? ReadHistoryJSON(DateTime date, IoBrokerHistory history)
        {
            var filePath = Path.Combine(InstanceContainer.ConfigManager.ZigbeeConfig.HistoryPath, $"{date:yyyyMMdd}", $"history.{AdapterWithId}.{history.PropertyName}.json");
            if (File.Exists(filePath))
            {
                var records = Newtonsoft.Json.JsonConvert.DeserializeObject<HistoryRecord[]>(File.ReadAllText(filePath));
                history.HistoryRecords = records!;
                return history;
            }
            return null;
        }

        private async Task<HistoryRecord[]> GetHistoryRecords(DateTimeOffset start, DateTimeOffset end, HistoryType type)
        {
            var i = AdapterWithId + "." + type.ToString().ToLower();

            var endMs = end.ToUnixTimeMilliseconds();
            HistoryRecord[]? a = null;
            using var slim = new ManualResetEventSlim(false);
            await Socket.EmitAsync("getHistory", response =>
            {
                a = response.GetValue<HistoryRecord[]>(1);
                slim.Set();
            },
            i,
            new
            {
                id = i, // probably not necessary to put it here again
                start = start.ToUnixTimeMilliseconds(),

                //end = end.ToUnixTimeMilliseconds(),
                ignoreNull = true,
                aggregate = "none",
                count = 200
            });
            slim.Wait();
            return a == null ? Array.Empty<HistoryRecord>() : a.Where(x => x.ts < endMs).ToArray();
        }
    }

    public enum HistoryType
    {
        Temperature,
        Humidity,
        Pressure
    }
}
