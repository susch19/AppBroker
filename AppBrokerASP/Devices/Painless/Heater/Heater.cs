using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Core.Database.Model;
using AppBroker.Core.Devices;
using AppBroker.Core.Models;

using AppBrokerASP.Devices.Zigbee;

using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json.Linq;

using NLog;
using NLog.Time;

using PainlessMesh;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DayOfWeek = AppBroker.Core.Models.DayOfWeek;

namespace AppBrokerASP.Devices.Painless.Heater;

[DeviceName("heater")]
[AppBroker.ClassPropertyChangedAppbroker]
public partial class Heater : PainlessDevice, IDisposable
{
    private HeaterConfig? temperature;
    private HeaterConfig? currentConfig;
    private HeaterConfig? currentCalibration;
    private long xiaomiTempSensor;
    private bool disableHeating;
    private bool disableLed;

    [AppBroker.IgnoreField]
    private readonly Task? heaterSensorMapping;
    [AppBroker.IgnoreField]
    private readonly List<HeaterConfig> timeTemps = new();
    [AppBroker.IgnoreField]
    private bool disposed;

    public Heater(long id, ByteLengthList parameters) : base(id, "PainlessMeshHeater")
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        timeTemps.AddRange(
            cont.HeaterConfigs
            .Include(x => x.Device)
            .Where(x => x.Device!.Id == id).
            Select(x => (HeaterConfig)x));

        ShowInApp = true;
        //cont.HeaterCalibrations.FirstOrDefault(x => x.Id == Id);
        try
        {

            DeviceModel? heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
            var mappings = cont.DeviceToDeviceMappings.Include(x => x.Child).Where(x => x.Parent!.Id == id /*&& cont.Devices.Any(y => y.Id == x.Child.Id)*/).ToList();
            Logger.Debug($"Heater {LogName} has {mappings?.Count} mappings");
            if (mappings?.Count > 0)
                heaterSensorMapping = Task.Run(() => TrySubscribe(mappings));
        }
        catch (Exception)
        {

        }

        Initialized = true;
        IInstanceContainer.Instance.DeviceStateManager.StateChanged += DeviceStateManager_StateChanged;
    }


    protected override void InterpretParameters(ByteLengthList parameters)
    {
        if (parameters is null)
            return;
        base.InterpretParameters(parameters);

        if (parameters.Count > 3)
        {
            try
            {
                byte[]? s = parameters[3];
                byte[] s2 = GetSendableTimeTemps(timeTemps);

                if (!s.SequenceEqual(s2))
                {
                    Logger.Warn($"Heater {LogName} has wrong temps saved, trying correcting");
                    var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Options, Command.Temp, s2);
                    InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Heater {LogName} has transmitted wrong temps");
            }
        }
        if (parameters.Count > 5)
        {
            DisableHeating = BitConverter.ToBoolean(parameters[4]);
            DisableLed = BitConverter.ToBoolean(parameters[5]);
        }
    }

    private static byte[] GetSendableTimeTemps(IEnumerable<HeaterConfig> timeTemps) => timeTemps.OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay).SelectMany(x => ((TimeTempMessageLE)x).ToBinary()).ToArray();

    private Task TrySubscribe(List<DeviceMappingModel> mappings)
    {
        while (mappings.Count > 0)
        {
            var toRemove = new List<DeviceMappingModel>();
            foreach (DeviceMappingModel? mapping in mappings)
            {

                if (mapping is not null && mapping.Child is null)
                {
                    toRemove.Add(mapping);
                    break;
                }

                AppBroker.Core.Devices.Device? device = IInstanceContainer.Instance.DeviceManager.Devices.FirstOrDefault(x => x.Key == mapping?.Child?.Id).Value;
                if (device != default)
                {
                    if (device is XiaomiTempSensor sensor)
                    {
                        SendLastTempData();
                        XiaomiTempSensor = device.Id;
                    }
                    Logger.Debug($"Heater {LogName} has subscribed to {device.Id}/{device.FriendlyName}");

                    if (mapping is not null)
                        toRemove.Add(mapping);
                    break;
                }
            }

            foreach (DeviceMappingModel? mapping in toRemove)
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
            case Command.Log:
                
                foreach (var item in e.Parameters)
                {
                    try
                    {

                        var seconds = BitConverter.ToInt64(item.AsSpan(0, sizeof(long)));

                        var dt = DateTime.UnixEpoch.AddSeconds(seconds);
                        var logLine = Encoding.UTF8.GetString(item.AsSpan(sizeof(long)));

                        Logger.Info($"{Id}|{FriendlyName}|{dt:yyyy-MM-dd HH:mm:ss}|{logLine}");
                    }
                    catch (Exception ex)
                    {
                        var logLine = Encoding.UTF8.GetString(item.AsSpan(sizeof(long)));

                        Logger.Info($"{Id}|{FriendlyName}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}-Unknown|{logLine}|Binary:{BitConverter.ToString(item)}");
                        
                    }
                }
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
        Span<byte> message = messages.AsSpan();
        var ttm = TimeTempMessageLE.LoadFromBinary(message[..3]);
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
            Logger.Error($"Heater {LogName} has Exception inside HandleTimeTempMessageUpdate\r\n {e} ");
            Console.WriteLine(e);
        }
    }

    public override Task UpdateFromApp(Command command, List<JToken> parameters)
    {
        Logger.Debug("UpdateFromApp " + command + " <> " + parameters.ToJson());
        BinarySmarthomeMessage msg;
        switch (command)
        {
            case Command.Temp:
                float temp = (float)parameters[0];
                var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);

                msg = new((uint)Id, MessageType.Update, command, ttm.ToBinary());
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            case Command.DeviceMapping:
                if (UpdateDeviceMappingInDb((long)parameters[0], (long)parameters[1]))
                    SendDataToAllSubscribers();
                break;
            case Command.Off:
                msg = new((uint)Id, MessageType.Update, command, new ByteLengthList());
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            case Command.On:
                msg = new((uint)Id, MessageType.Update, command, new ByteLengthList());
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            default:
                break;
        }

        return Task.CompletedTask;
    }

    public override void OptionsFromApp(Command command, List<JToken> parameters)
    {

        Logger.Debug($"{Id}|OptionsFromApp {command} <> {parameters.ToJson()}");
        BinarySmarthomeMessage msg;

        switch (command)
        {
            case Command.Temp:
            {
                IOrderedEnumerable<HeaterConfig>? hc = parameters.Select(x => x.ToDeObject<HeaterConfig>()).OrderBy(x => x.DayOfWeek).ThenBy(x => x.TimeOfDay);

                UpdateDB(hc);

                IEnumerable<TimeTempMessageLE>? ttm = hc.Select(x => new TimeTempMessageLE(x.DayOfWeek, new TimeSpan(x.TimeOfDay.Hour, x.TimeOfDay.Minute, 0), (float)x.Temperature));
                byte[]? s = ttm.SelectMany(x => x.ToBinary()).ToArray();

                msg = new((uint)Id, MessageType.Options, command, s);
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            }
            case Command.Off:
                msg = new((uint)Id, MessageType.Options, command, new ByteLengthList());
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            case Command.On:
                msg = new((uint)Id, MessageType.Options, command, new ByteLengthList());
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
            case Command.Mode:
                msg = new((uint)Id, MessageType.Options, command);
                InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
                break;
        }
    }

    private void UpdateDB(IEnumerable<HeaterConfig> hc)
    {
        timeTemps.Clear();
        timeTemps.AddRange(hc);

        var models = hc.Select(x => (HeaterConfigModel)x).ToList();
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        DeviceModel? d = cont.Devices.FirstOrDefault(x => x.Id == Id);
        if (d is not null)
        {
            foreach (HeaterConfigModel? item in models)
                item.Device = d;
        }

        var oldConfs
            = cont
            .HeaterConfigs
            .Where(x => x.DeviceId == Id)
            .ToList();

        if (oldConfs.Count > 0)
        {
            cont.RemoveRange(oldConfs);
            _ = cont.SaveChanges();
        }
        cont.AddRange(models);
        _ = cont.SaveChanges();
    }


    private bool UpdateDeviceMappingInDb(long tempId, long oldId)
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        DeviceModel? heater = cont.Devices.FirstOrDefault(x => x.Id == Id);
        DeviceModel? tempSensor = cont.Devices.FirstOrDefault(x => x.Id == tempId);
        AppBroker.Core.Devices.Device? sensor = IInstanceContainer.Instance.DeviceManager.Devices.FirstOrDefault(x => x.Key == tempId).Value;
        if (heater == default || tempSensor == default || sensor == default)
            return false;

        IEnumerable<DeviceMappingModel>? oldMappings = ((IEnumerable<DeviceMappingModel>)cont.DeviceToDeviceMappings).Where(x => x.Parent!.Id == Id);
        foreach (DeviceMappingModel? oldMapping in oldMappings)
        {
            _ = cont.Remove(oldMapping);
        }
        _ = cont.SaveChanges();

        _ = cont.Add(new DeviceMappingModel
        {
            Parent = heater,
            Child = tempSensor
        });

        XiaomiTempSensor = sensor.Id;
        _ = cont.SaveChanges();
        return true;
    }



    private void DeviceStateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (e.Id != XiaomiTempSensor || e.PropertyName != "Temperature")
            return;

        var temp = e.NewValue.Value<float>();
        var ttm = new TimeTempMessageLE((DayOfWeek)((((byte)DateTime.Now.DayOfWeek) + 6) % 7), new TimeSpan(DateTime.Now.TimeOfDay.Hours, DateTime.Now.TimeOfDay.Minutes, 0), temp);
        var msg = new BinarySmarthomeMessage((uint)Id, MessageType.Relay, Command.Temp, ttm.ToBinary());
        InstanceContainer.Instance.MeshManager.SendSingle((uint)Id, msg);
    }

    public override void Reconnect(ByteLengthList parameter)
    {
        base.Reconnect(parameter);

        InterpretParameters(parameter);

        InstanceContainer.Instance.MeshManager.SingleUpdateMessageReceived -= Node_SingleUpdateMessageReceived;
        InstanceContainer.Instance.MeshManager.SingleOptionsMessageReceived -= Node_SingleOptionsMessageReceived;
        InstanceContainer.Instance.MeshManager.SingleUpdateMessageReceived += Node_SingleUpdateMessageReceived;
        InstanceContainer.Instance.MeshManager.SingleOptionsMessageReceived += Node_SingleOptionsMessageReceived;

        SendLastTempData();
    }

    public override dynamic GetConfig() => timeTemps.ToJson();

    private void SendLastTempData()
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(XiaomiTempSensor, out AppBroker.Core.Devices.Device? device)
                && device is XiaomiTempSensor sensor)
        {
            var existing = IInstanceContainer.Instance.DeviceStateManager.GetSingleState(sensor.Id
                , "temperature");
            if (existing is not null)
                DeviceStateManager_StateChanged(this, new(sensor.Id, "temperature", null, existing));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                heaterSensorMapping?.Dispose();
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
