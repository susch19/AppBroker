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
using Microsoft.EntityFrameworkCore;
using AppBokerASP.Devices.Zigbee;
using System.Buffers.Text;
using NLog;

namespace AppBokerASP.Devices.Painless.Heater
{
    public class Heater : PainlessDevice, IDisposable
    {

        public HeaterConfig Temperature { get; set; }
        public long XiaomiTempSensor { get; set; }
        public HeaterConfig CurrentConfig { get; set; }
        public HeaterConfig CurrentCalibration { get; private set; }

        private readonly List<HeaterConfig> timeTemps = new List<HeaterConfig>();

        private string logName => Id + "/" + FriendlyName;

        private Task heaterSensorMapping;

        //2020-02-01 20:14:48.1075|DEBUG|AppBokerASP.BaseClient|{"id":3257233774, "m":"Update", "c":"WhoIAm", "p":["10.12.206.9","heater","RmlybXdhcmUgVjIgRmViICAxIDIwMjA=","YAk3oJQqQBo3QKkqYQk3oZQqQRo3QakqYgk3opQqQho3QqkqYwk3o5QqQxo3Q6kqZAk3pJQqRBo3RKkqJQ03RakqJg03Rqkq"]}
        public Heater(long id, List<string> parameters) : base(id)
        {
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;
            using var cont = DbProvider.BrokerDbContext;
            timeTemps.AddRange(cont.HeaterConfigs.Where(x => x.Device.Id == id).ToList().Select(x => (HeaterConfig)x));
            InterpretParameters(parameters);

            ShowInApp = true;
            //cont.HeaterCalibrations.FirstOrDefault(x => x.Id == Id);

            var heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
            var mappings = cont.DeviceToDeviceMappings.Include(x => x.Child).Where(x => x.Parent.Id == id /*&& cont.Devices.Any(y => y.Id == x.Child.Id)*/).ToList();
            logger.Debug($"Heater {logName} has {mappings.Count} mappings");
            if (mappings.Count > 0)
                heaterSensorMapping = Task.Run(() => TrySubscribe(mappings));
        }

        protected override void InterpretParameters(List<string> parameters)
        {
            if (parameters is null)
                return;
            base.InterpretParameters(parameters);
            //if (parameters.Count > 2)
            //{
                //var s2 = "";
                //timeTemps.OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay).ToList().ForEach(x => s2 += ((TimeTempMessageLE)x).ToString());
                //var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, Command.Temp, s2.ToJToken());
                //Program.MeshManager.SendSingle((uint)Id, msg);

            //}
            if (parameters.Count > 3)
            {
                try
                {
                    var s = parameters[3];
                    var s2 = "";
                    timeTemps.OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay).ToList().ForEach(x => s2 += ((TimeTempMessageLE)x).ToString());

                    if (s != s2)
                    {
                        logger.Warn($"Heater {logName} has wrong temps saved, trying to correct Saved:{{{s}}} Server:{{{s2}}}");
                        var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, Command.Temp, s2.ToJToken());
                        Program.MeshManager.SendSingle((uint)Id, msg);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Heater {logName} has transmitted wrong temps");
                }
            }
        }


        private Task TrySubscribe(List<DeviceMappingModel> mappings)
        {
            while (mappings.Count > 0)
            {
                var toRemove = new List<DeviceMappingModel>();
                foreach (var mapping in mappings)
                {

                    if (mapping?.Child == null)
                    {
                        toRemove.Add(mapping);
                        break;
                    }

                    var device = Program.DeviceManager.Devices.FirstOrDefault(x => x.Key == mapping?.Child?.Id).Value;
                    if (device != default)
                    {
                        if (device is XiaomiTempSensor sensor)
                        {
                            sensor.TemperatureChanged += XiaomiTempSensorTemperaturChanged;
                            SendLastTempData();
                            XiaomiTempSensor = device.Id;
                        }
                        logger.Debug($"Heater {logName} has subscribed to {device.Id}/{device.FriendlyName}");

                        toRemove.Add(mapping);
                        break;
                    }
                }

                foreach (var mapping in toRemove)
                {
                    mappings.Remove(mapping);
                }

                Thread.Sleep(1000);
            }
            return Task.CompletedTask;
        }

        private void Node_SingleUpdateMessageReceived(object sender, GeneralSmarthomeMessage e)
        {
            if (e.NodeId != Id)
                return;
            logger.Debug($"DataReceived in {nameof(Node_SingleUpdateMessageReceived)} {logName}: " + e.ToJson());
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

            logger.Debug($"DataReceived in {nameof(Node_SingleOptionsMessageReceived)} {logName}: " + e.ToJson());
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
                logger.Error($"Heater {logName} has Exception inside HandleTimeTempMessageUpdate\r\n {e} ");
                Console.WriteLine(e);
            }

            SendDataToAllSubscribers();
        }


        public override void UpdateFromApp(Command command, List<JToken> parameters)
        {

            logger.Debug("UpdateFromApp " + command + " <> " + parameters.ToJson());
            switch (command)
            {
                case Command.Temp:
                    var temp = (float)parameters[0];
                    var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
                    var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Update, command, ttm.ToString().ToJToken());
                    Program.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.DeviceMapping:
                    UpdateDeviceMappingInDb((long)parameters[0], (long)parameters[1]);
                    break;
                default:
                    break;
            }
        }


        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {

            logger.Debug("OptionsFromApp " + command + " <> " + parameters.ToJson());
            GeneralSmarthomeMessage msg;
            switch (command)
            {
                case Command.Temp:
                    {
                        var hc = parameters.Select(x => x.ToDeObject<HeaterConfig>()).OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay);

                        UpdateDB(hc);

                        var ttm = hc.Select(x => new TimeTempMessageLE(x.DayOfWeek, new TimeSpan(x.TimeOfDay.Hour, x.TimeOfDay.Minute, 0), (float)x.Temperature));
                        var s = "";
                        foreach (var item in ttm)
                            s += item.ToString();

                        msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, command, s.ToJToken());
                        Program.MeshManager.SendSingle((uint)Id, msg);
                        break;
                    }

                case Command.Mode:
                    msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, command);
                    Program.MeshManager.SendSingle((uint)Id, msg);
                    break;
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

        private void UpdateDeviceMappingInDb(long tempId, long oldId)
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
            base.StopDevice();
            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
        }

        public override void Reconnect(List<string>? parameter)
        {
            base.Reconnect(parameter);

            InterpretParameters(parameter);

            Program.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
            Program.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            Program.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;

            SendLastTempData();
        }


        public override dynamic GetConfig() => timeTemps.ToJson();
        public void Dispose()
        {
            heaterSensorMapping?.Dispose();
        }

        private void SendLastTempData()
        {
            if (Program.DeviceManager.Devices.TryGetValue(XiaomiTempSensor, out var device)
                    && device is XiaomiTempSensor sensor && sensor.Temperature > 5f)
                XiaomiTempSensorTemperaturChanged(this, sensor.Temperature);
        }
    }
}
