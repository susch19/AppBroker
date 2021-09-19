using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PainlessMesh;
using AppBrokerASP.Database;
using AppBrokerASP.Database.Model;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;
using AppBrokerASP.Devices.Zigbee;
using System.Diagnostics.CodeAnalysis;

namespace AppBrokerASP.Devices.Painless.Heater
{
    [DeviceName("heater")]
    public class Heater : PainlessDevice
    {
        public HeaterConfig? Temperature { get; set; }
        public long XiaomiTempSensor { get; set; } = 0;
        public HeaterConfig? CurrentConfig { get; set; }
        public HeaterConfig? CurrentCalibration { get; private set; }

        private readonly List<HeaterConfig> timeTemps = new();

        public bool DisableHeating { get; set; }
        public bool DisableLed { get; set; }

        private readonly Task? heaterSensorMapping;

        //2020-02-01 20:14:48.1075|DEBUG|AppBrokerASP.BaseClient|{"id":3257233774, "m":"Update", "c":"WhoIAm", "p":["10.12.206.9","heater","RmlybXdhcmUgVjIgRmViICAxIDIwMjA=","YAk3oJQqQBo3QKkqYQk3oZQqQRo3QakqYgk3opQqQho3QqkqYwk3o5QqQxo3Q6kqZAk3pJQqRBo3RKkqJQ03RakqJg03Rqkq"]}
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Dynamicly created")]
        public Heater(long id, ByteLengthList parameters) : base(id)
        {
            using var cont = DbProvider.BrokerDbContext;
            timeTemps.AddRange(cont.HeaterConfigs.Where(x => x.Device!.Id == id).ToList().Select(x => (HeaterConfig)x));

            ShowInApp = true;
            //cont.HeaterCalibrations.FirstOrDefault(x => x.Id == Id);
            try
            {

                var heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
                var mappings = cont.DeviceToDeviceMappings.Include(x => x.Child).Where(x => x.Parent!.Id == id /*&& cont.Devices.Any(y => y.Id == x.Child.Id)*/).ToList();
                logger.Debug($"Heater {LogName} has {mappings?.Count} mappings");
                if (mappings?.Count > 0)
                    heaterSensorMapping = Task.Run(() => TrySubscribe(mappings));
            }
            catch (Exception ex)
            {

            }
        }

        protected override void InterpretParameters(ByteLengthList parameters)
        {
            if (parameters is null)
                return;
            base.InterpretParameters(parameters);
            //if (parameters.Count > 2)
            //{
            //var s2 = "";
            //timeTemps.OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay).ToList().ForEach(x => s2 += ((TimeTempMessageLE)x).ToString());
            //var msg = new GeneralSmarthomeMessage((uint)Id, MessageType.Options, Command.Temp, s2.ToJToken());
            //InstanceContainer.MeshManager.SendSingle((uint)Id, msg);

            //}
            if (parameters.Count > 3)
            {
                try
                {
                    var s = parameters[3];
                    byte[] s2 = GetSendableTimeTemps(timeTemps);

                    if (!s.SequenceEqual(s2))
                    {
                        logger.Warn($"Heater {LogName} has wrong temps saved, trying correcting");
                        var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Options, Command.Temp, s2);
                        InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, $"Heater {LogName} has transmitted wrong temps");
                }
            }
            if (parameters.Count > 5)
            {
                DisableHeating = BitConverter.ToBoolean(parameters[4]);
                DisableLed = BitConverter.ToBoolean(parameters[5]);
            }
        }

        private static byte[] GetSendableTimeTemps(IEnumerable<HeaterConfig> timeTemps)
        {
            return timeTemps.OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay).SelectMany(x => ((TimeTempMessageLE)x).ToBinary()).ToArray();
        }

        private Task TrySubscribe(List<DeviceMappingModel> mappings)
        {
            while (mappings.Count > 0)
            {
                var toRemove = new List<DeviceMappingModel>();
                foreach (var mapping in mappings)
                {

                    if (mapping is not null && mapping.Child is null)
                    {
                        toRemove.Add(mapping);
                        break;
                    }

                    var device = InstanceContainer.DeviceManager.Devices.FirstOrDefault(x => x.Key == mapping?.Child?.Id).Value;
                    if (device != default)
                    {
                        if (device is XiaomiTempSensor sensor)
                        {
                            sensor.TemperatureChanged += XiaomiTempSensorTemperaturChanged;
                            SendLastTempData();
                            XiaomiTempSensor = device.Id;
                        }
                        logger.Debug($"Heater {LogName} has subscribed to {device.Id}/{device.FriendlyName}");

                        if (mapping is not null)
                            toRemove.Add(mapping);
                        break;
                    }
                }

                foreach (var mapping in toRemove)
                {
                    _ = mappings.Remove(mapping);
                }

                Thread.Sleep(1000);
            }
            return Task.CompletedTask;
        }

        protected override void UpdateMessageReceived(BinarySmarthomeMessage e)
        {

            switch (e.Command)
            {
                case Command.Temp:
                    HandleTimeTempMessageUpdate(e.Parameters[0]);
                    break;
                case Command.Off:
                    DisableHeating = true;
                    break;
                case Command.On:
                    DisableHeating = false;
                    break;
                default:
                    break;
            }
            SendDataToAllSubscribers();
        }

        protected override void OptionMessageReceived(BinarySmarthomeMessage e)
        {
            switch (e.Command)
            {
                case Command.Temp:
                    //CurrentConfig = TimeTempMessageLE.FromBase64((string)e.Parameters[0]);
                    //SendDataToAllSubscribers();
                    break;
                case Command.Off:
                    DisableLed = true;
                    break;
                case Command.On:
                    DisableLed = false;
                    break;
                case Command.Mode:
                    //if (e.Parameters.Count < 1)
                    //    return;
                    //DisableLed = BitConverter.ToBoolean(e.Parameters[0]);
                    break;
                default:

                    break;
            }
            SendDataToAllSubscribers();
        }

        private void HandleTimeTempMessageUpdate(byte[] messages)
        {
            var message = messages.AsSpan();
            var ttm = TimeTempMessageLE.LoadFromBinary(message[0..3]);
            Temperature = ttm;
            ttm = TimeTempMessageLE.LoadFromBinary(message[3..6]);
            CurrentConfig = ttm;
            ttm = TimeTempMessageLE.LoadFromBinary(message[6..9]);
            try
            {
                CurrentCalibration = ttm;
                CurrentCalibration.Temperature -= 51.2f;
            }
            catch (Exception e)
            {
                logger.Error($"Heater {LogName} has Exception inside HandleTimeTempMessageUpdate\r\n {e} ");
                Console.WriteLine(e);
            }
        }


        public override Task UpdateFromApp(Command command, List<JToken> parameters)
        {

            logger.Debug("UpdateFromApp " + command + " <> " + parameters.ToJson());
            BinarySmarthomeMessage msg;
            switch (command)
            {
                case Command.Temp:
                    var temp = (float)parameters[0];
                    var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
                    //logger.Debug("Send new ttm: " + )
                    msg = new((uint)Id, MessageType.Update, command, ttm.ToBinary());
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.DeviceMapping:
                    UpdateDeviceMappingInDb((long)parameters[0], (long)parameters[1]);
                    break;
                case Command.Off:
                    msg = new((uint)Id, MessageType.Update, command, new ByteLengthList());
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.On:
                    msg = new((uint)Id, MessageType.Update, command, new ByteLengthList());
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    break;
                default:
                    break;
            }

            return Task.CompletedTask;
        }


        public override void OptionsFromApp(Command command, List<JToken> parameters)
        {

            logger.Debug("OptionsFromApp " + command + " <> " + parameters.ToJson());
            BinarySmarthomeMessage msg;

            switch (command)
            {
                case Command.Temp:
                    {
                        var hc = parameters.Select(x => x.ToDeObject<HeaterConfig>()).OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay);

                        UpdateDB(hc);

                        var ttm = hc.Select(x => new TimeTempMessageLE(x.DayOfWeek, new TimeSpan(x.TimeOfDay.Hour, x.TimeOfDay.Minute, 0), (float)x.Temperature));
                        var s = ttm.SelectMany(x => x.ToBinary()).ToArray();

                        msg = new ((uint)Id, MessageType.Options, command, s);
                        InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                        break;
                    }
                case Command.Off:
                    msg = new ((uint)Id, MessageType.Options, command, new ByteLengthList());
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.On:
                    msg = new ((uint)Id, MessageType.Options, command, new ByteLengthList());
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
                    break;
                case Command.Mode:
                    msg = new ((uint)Id, MessageType.Options, command);
                    InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
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
            if (d is not null)
                foreach (var item in models)
                    item.Device = d;

            var oldConfs = cont.HeaterConfigs.Where(x => x.Device!.Id == Id).ToList();
            if (oldConfs.Count > 0)
            {
                cont.RemoveRange(oldConfs);
                _ = cont.SaveChanges();
            }
            cont.AddRange(models);
            _ = cont.SaveChanges();
        }

        private void UpdateDeviceMappingInDb(long tempId, long oldId)
        {
            using var cont = DbProvider.BrokerDbContext;
            var heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
            var tempSensor = cont.Devices.FirstOrDefault(x => x.Id == tempId);
            var sensor = InstanceContainer.DeviceManager.Devices.FirstOrDefault(x => x.Key == tempId).Value;
            if (heater == default || tempSensor == default || sensor == default)
                return;

            var oldMappings = cont.DeviceToDeviceMappings.Where(x => x.Parent!.Id == Id);
            foreach (var oldMapping in oldMappings)
            {
                var oldsensor = InstanceContainer.DeviceManager.Devices.FirstOrDefault(x => x.Key == oldId).Value;
                if (oldsensor is not null and XiaomiTempSensor xts)
                    xts.TemperatureChanged -= XiaomiTempSensorTemperaturChanged;
                _ = cont.Remove(oldMapping);
            }
            _ = cont.SaveChanges();


            _ = cont.Add(new DeviceMappingModel
            {
                Parent = heater,
                Child = tempSensor
            });

            if (sensor is XiaomiTempSensor xiaomiTempSensor)
                xiaomiTempSensor.TemperatureChanged += XiaomiTempSensorTemperaturChanged;
            XiaomiTempSensor = sensor.Id;
            _ = cont.SaveChanges();
        }

        private void XiaomiTempSensorTemperaturChanged(object? sender, float temp)
        {
            var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
            var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Relay, Command.Temp, ttm.ToBinary());
            InstanceContainer.MeshManager.SendSingle((uint)Id, msg);
        }

        public override void Reconnect(ByteLengthList parameter)
        {
            base.Reconnect(parameter);

            InterpretParameters(parameter);

            InstanceContainer.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
            InstanceContainer.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
            InstanceContainer.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
            InstanceContainer.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;

            SendLastTempData();
        }

        public override dynamic GetConfig() => timeTemps.ToJson();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                heaterSensorMapping?.Dispose();
            }

            base.Dispose(disposing);
        }

        private void SendLastTempData()
        {
            if (InstanceContainer.DeviceManager.Devices.TryGetValue(XiaomiTempSensor, out var device)
                    && device is XiaomiTempSensor sensor && sensor.Temperature > 5f)
                XiaomiTempSensorTemperaturChanged(this, sensor.Temperature);
        }
    }
}
