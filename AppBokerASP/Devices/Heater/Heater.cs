using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using PainlessMesh;
using Newtonsoft.Json;
using AppBokerASP.Database;
using AppBokerASP.Database.Model;
using Newtonsoft.Json.Linq;

namespace AppBokerASP.Devices.Heater
{
    public class Heater : Device, IDisposable
    {
        public HeaterConfig Temperature { get; set; }
        public ulong XiaomiTempSensor { get; set; }
        public HeaterConfig CurrentConfig { get; set; }
        public HeaterConfig CurrentCalibration { get; private set; }

        private readonly List<HeaterConfig> timeTemps = new List<HeaterConfig>();

        private Task heaterSensorMapping;
        private bool heaterSensorMappingStop;


        public Heater() : base(0)
        {
            ShowInApp = true;
        }

        public Heater(uint id) : base(id)
        {
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            using var cont = DbProvider.BrokerDbContext;
            timeTemps.AddRange(cont.HeaterConfigs.Where(x => x.Device.Id == id).ToList().Select(x => (HeaterConfig)x));
            ShowInApp = true;

            var heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
            var mapping = cont.DeviceToDeviceMappings.FirstOrDefault(x => x.Parent == heater && cont.Devices.Any(y => y.Id == x.Child.Id));
            if (mapping != default)
                heaterSensorMapping = TrySubscribe(mapping);
        }


        private Task TrySubscribe(DeviceMappingModel mapping)
        {
            while (!heaterSensorMappingStop)
            {
                if (mapping?.Child == null)
                {
                    heaterSensorMappingStop = true;
                    break;
                }
                var device = Program.DeviceManager.Devices.FirstOrDefault(x => x.Key == mapping?.Child?.Id).Value;
                if (device != default)
                {
                    ((XiaomiTempSensor)device).TemperatureChanged += XiaomiTempSensorTemperaturChanged;
                    XiaomiTempSensor = device.Id;
                    heaterSensorMappingStop = true;
                }
                Thread.Sleep(1000);
            }
            heaterSensorMapping?.Dispose();
            return Task.CompletedTask;
        }

        private void Node_SingleUpdateMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            switch (e.Command)
            {
                case Command.Temp:
                    HandleTimeTempMessageUpdate((string)e.Parameters[0]);
                    break;
                default:
                    break;
            }
        }
        private void Node_SingleOptionsMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            switch (e.Command)
            {
                case Command.Temp:
                    //CurrentConfig = TimeTempMessageLE.FromBase64((string)e.Parameters[0]);
                    //SendDataToAllSubscribers();
                    break;
                default:
                    break;
            }
        }

        private void HandleTimeTempMessageUpdate(string messages)
        {
            var ttm = TimeTempMessageLE.FromBase64(messages[0..4]);
            Temperature = ttm;
            ttm = TimeTempMessageLE.FromBase64(messages[4..8]);
            CurrentConfig = ttm;
            ttm = TimeTempMessageLE.FromBase64(messages[8..12]);
            try
            {
                var dt = DateTime.Now;
                dt.AddHours(ttm.Time.Hours - dt.Hour);
                dt.AddMinutes(ttm.Time.Minutes - dt.Minute);

                CurrentCalibration = new HeaterConfig(ttm.DayOfWeek, dt, ttm.Temp - 51.2f);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            SendDataToAllSubscribers();
        }


        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {

            switch (command)
            {
                case Command.Temp:
                    var temp = (float)parameters[0];
                    var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
                    var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Update, command, ttm.ToString().ToJToken());
                    Program.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.DeviceMapping:
                    UpdateDeviceMappingInDb((ulong)parameters[0], (ulong)parameters[1]);
                    break;
                default:
                    break;
            }
        }


        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {

            if (command == Command.Temp)
            {
                var hc = parameters.Select(x => x.ToDeObject<HeaterConfig>()).OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay);

                UpdateDB(hc);

                var ttm = hc.Select(x => new TimeTempMessageLE(x.DayOfWeek, new TimeSpan(x.TimeOfDay.Hour, x.TimeOfDay.Minute, 0), (float)x.Temperature));
                var s = "";
                foreach (var item in ttm)
                    s += item.ToString();

                var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, command, s.ToJToken());
                Program.MeshManager.SendSingle((uint)Id, msg);
            }
        }

        private void UpdateDB(IEnumerable<HeaterConfig> hc)
        {
            timeTemps.Clear();
            timeTemps.AddRange(hc);

            var models = hc.Select(x => (HeaterConfigModel)x).ToList();
            using var cont = DbProvider.BrokerDbContext;
            var d = cont.Devices.FirstOrDefault(x => x.Id == Id);
            foreach (var item in models)
                item.Device = d;

            var oldConfs = cont.HeaterConfigs.Where(x => x.Device.Id == Id).ToList();
            if (oldConfs.Count > 0)
            {
                cont.RemoveRange(oldConfs);
                cont.SaveChanges();
            }
            cont.AddRange(models);
            cont.SaveChanges();
        }

        private void UpdateDeviceMappingInDb(ulong tempId, ulong oldId)
        {
            using var cont = DbProvider.BrokerDbContext;
            var heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
            var tempSensor = cont.Devices.FirstOrDefault(x => x.Id == tempId);
            var sensor = Program.DeviceManager.Devices.FirstOrDefault(x => x.Key == tempId).Value;
            if (heater == default || tempSensor == default || sensor == default)
                return;


            var oldMappings = cont.DeviceToDeviceMappings.Where(x => x.Parent.Id == Id);
            foreach (var oldMapping in oldMappings)
            {
                var oldsensor = Program.DeviceManager.Devices.FirstOrDefault(x => x.Key == oldId).Value;
                if (oldsensor != default)
                    ((XiaomiTempSensor)oldsensor).TemperatureChanged -= XiaomiTempSensorTemperaturChanged;
                cont.Remove(oldMapping);
            }
            cont.SaveChanges();


            cont.Add(new DeviceMappingModel
            {
                Parent = heater,
                Child = tempSensor
            });
            (sensor as XiaomiTempSensor).TemperatureChanged += XiaomiTempSensorTemperaturChanged;
            XiaomiTempSensor = sensor.Id;
            cont.SaveChanges();
            heaterSensorMappingStop = true;
        }

        private void XiaomiTempSensorTemperaturChanged(object sender, float temp)
        {
            var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
            var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Relay, Command.Temp, ttm.ToString().ToJToken());
            Program.MeshManager.SendSingle((uint)Id, msg);
        }

        public void Update(string message)
        {

        }

        public override void StopDevice()
        {
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
        }

        public override dynamic GetConfig() => timeTemps.ToJson();
        public void Dispose()
        {
            heaterSensorMapping?.Dispose();
        }
    }
}
