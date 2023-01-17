using AppBroker.Core.Devices;
using AppBroker.Zigbee2Mqtt.Devices;

using Newtonsoft.Json.Linq;
using AppBroker.Core.Models;
using AppBroker.Core.Database;
using AppBroker.Core.Database.Model;
using DayOfWeek = AppBroker.Core.Models.DayOfWeek;
using AppBroker.maxi.Devices;
using Microsoft.EntityFrameworkCore;
using AppBroker.Core;
using AppBroker.Zigbee2Mqtt;
using System.Reactive.Linq;
using dotVariant;
using NLog;

namespace AppBroker.maxi;

[Variant]
internal partial struct RoomStateChange
{
    static partial void VariantOf(float temperature, bool isOpen, IHeaterConfigModel heaterConfigModel);
}

internal static class RoomFactory
{

    public static IObservable<Room> Create(string name, Logger logger, IDeviceStateManager stateManager, Zigbee2MqttManager zigbee2Mqtt, Device mainDevice, params Device[] devices)
    {
        if (!devices.Contains(mainDevice))
        {
            logger.Warn("Main device is not part of devices. Main Device added automaticly");
            var oldDevices = devices;
            devices = new Device[oldDevices.Length];
            Array.Copy(oldDevices, 0, devices, 0, oldDevices.Length);
            devices[^1] = mainDevice;
        }

        var currentConfig = GetCurrentHeaterConfig(mainDevice.Id);

        logger.Debug($"Current heater config for {name} is {(currentConfig?.Temperature.ToString() ?? "null")}");

        var room = new Room(name, mainDevice, devices, stateManager, zigbee2Mqtt, 0.0f, false, currentConfig);

        var init
                = Observable
                    .Return(-1L);

        var time
                = Observable
                    .Interval(TimeSpan.FromMinutes(5));

        var changesByTime
                    = Observable
                    .Concat(init, time)
                    .Do(_ => logger.Trace($"Get Room Update for {name} by time"))
                    .SelectMany(_ => GetRoomUpdates(room));

        //var deviceStateChanges
        //    = Observable
        //    .FromEventPattern<StateChangeArgs>(
        //        add => stateManager.StateChanged += add,
        //        remove => stateManager.StateChanged -= remove
        //    )
        //    .Select(eventPattern => eventPattern.EventArgs);

        var temperatureChanges
            = FilterDevices(devices, IsAverageTemperature)
            .OfType<GroupingDevice<float>>()
            .Select(
                groupDevice
                    => Observable
                    .FromEventPattern<float>(
                        add => groupDevice.ValueChanged += add,
                        remove => groupDevice.ValueChanged -= remove
                    )
            )
            .Merge()
            .Throttle(TimeSpan.FromMinutes(2))
            .Do(value => logger.Debug($"room {name} gets new average Temperature: {value.EventArgs}"))
            .Select(eventPattern => new RoomStateChange(eventPattern.EventArgs));

        var contactChanges
            = FilterDevices(devices, IsContactGroup)
            .OfType<GroupingDevice<byte>>()
            .Select(
                groupDevice
                    => Observable
                    .FromEventPattern<byte>(
                        add => groupDevice.ValueChanged += add,
                        remove => groupDevice.ValueChanged -= remove
                    )
            )
            .Merge()
            .Do(value => logger.Debug($"room {name} gets new contact state Contact: {value.EventArgs < 1}"))
            .Select(eventPattern => new RoomStateChange(eventPattern.EventArgs < 1));

        var stateChanges
            = Observable
            .Merge(changesByTime, temperatureChanges, contactChanges);

        return stateChanges
            .Do(_ => logger.Trace($"rcv change message in {name}"))
            .Scan(room, (room, change)
                => change
                    .Visit(
                        (float average) =>
                            {
                                var currentTemperature = Math.Round(average, 2);

                                if (room.CurrentTemperature == currentTemperature)
                                    return room;

                                var newRoom = room with { CurrentTemperature = currentTemperature };

                                if (newRoom.IsOpen)
                                    return newRoom;

                                CalibrateTemperature(newRoom);

                                logger.Debug($"room {name} changed current temperature form {room.CurrentTemperature} to {newRoom.CurrentTemperature}");
                                return newRoom;
                            },
                        (bool isOpen) =>
                            {
                                if (room.IsOpen == isOpen)
                                    return room;

                                var newRoom = room with { IsOpen = isOpen };
                                UpdateRoomHeaterOpenWindow(newRoom);

                                logger.Debug($"room {name} changed current open state form {room.IsOpen} to {newRoom.IsOpen}");
                                return newRoom;
                            },
                        (IHeaterConfigModel model) =>
                            {
                                if (IsEquals(room.CurrentPlan, model))
                                    return room;

                                var newRoom = room with { CurrentPlan = model };

                                if (newRoom.IsOpen)
                                    return newRoom;

                                SetTargetTemperature(newRoom);

                                logger.Debug($"room {name} changed current plan form {(room.CurrentPlan?.Temperature.ToString() ?? "null")} to {(newRoom.CurrentPlan?.Temperature.ToString() ?? "null")}");
                                return newRoom;
                            }
                     )
            )
            .DistinctUntilChanged()
            .Do(room => logger.Info($"chg room state {room.CurrentTemperature} °C, {(room.IsOpen ? "open" : "closed")}, plan {room.CurrentPlan?.Temperature.ToString() ?? "null"} °C"));
    }

    private static bool IsEquals(IHeaterConfigModel? left, IHeaterConfigModel? right)
    {
        if (Equals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.DayOfWeek == right.DayOfWeek
            && left.TimeOfDay.Ticks == right.TimeOfDay.Ticks
            && left.Temperature == right.Temperature;
    }

    private static IEnumerable<RoomStateChange> GetRoomUpdates(Room room)
    {
        yield return IsRoomOpen(room.Devices);
        yield return FilterDevices(room.Devices, IsAverageTemperature).OfType<GroupingDevice<float>>().First().Value;

        var currentHeaterConfig = GetCurrentHeaterConfig(room.MainDevice.Id);

        if (currentHeaterConfig is not null)
            yield return new RoomStateChange(currentHeaterConfig);
    }

    private static void CalibrateTemperature(Room room)
    {
        const double maxPossibleOffset = 20d;

        FilterDevices(room.Devices, IsHeater)
            .OfType<Zigbee2MqttDevice>()
            .Select(device => (device, localTemperature: GetLocalTemperature(room.StateManager, device), calibration: GetLocalCalibration(room.StateManager, device)))
            .ToList()
            .ForEach(temperatureInfo =>
            {
                var uncalibratedTemperature = temperatureInfo.localTemperature - temperatureInfo.calibration;
                var offset = Math.Round(room.CurrentTemperature - uncalibratedTemperature, 2);

                if(offset > maxPossibleOffset || offset < (maxPossibleOffset * -1))
                {
                    offset = 0;
                }
                else
                {
                    room.StateManager.PushNewState(temperatureInfo.device.Id, "unoccupied_local_temperature", uncalibratedTemperature);
                    _ = room.Zigbee2Mqtt.SetValue(temperatureInfo.device.FriendlyName, "unoccupied_local_temperature", uncalibratedTemperature);
                }                

                if (offset != temperatureInfo.calibration)
                {
                    _ = room.Zigbee2Mqtt.SetValue(temperatureInfo.device.FriendlyName, "local_temperature_calibration", offset);
                }
            });
    }

    private static void SetTargetTemperature(Room room)
    {
        FilterDevices(room.Devices, IsHeater)
            .OfType<Zigbee2MqttDevice>()
            .Select(device => (device, config: GetCurrentHeaterConfig(device.Id) ?? room.CurrentPlan))
            .Where(heatingEntry => heatingEntry.config is not null)
            .ToList()
            .ForEach(heatingEntry =>
            {
                _ = room.Zigbee2Mqtt.SetValue(heatingEntry.device.FriendlyName, "current_heating_setpoint", heatingEntry.config!.Temperature);
            });
    }

    private static void UpdateRoomHeaterOpenWindow(Room room)
    {
        var climates
            = FilterDevices(room.Devices, IsHeater)
               .OfType<Zigbee2MqttDevice>()
               .ToList();

        var mode = room.IsOpen ? "off" : "auto";

        foreach (var device in climates)
        {
            device.SetDeviceAndZigbeeProperty(room, "system_mode", mode);

            //if (room.IsOpen)
            //{
            //    _ = room.Zigbee2Mqtt.SetValue(device.FriendlyName, "eurotronic_host_flags", JToken.FromObject(new { window_open = true }));
            //    device.SetDeviceAndZigbeeProperty(room, "trv_mode", 1);
            //    device.SetDeviceAndZigbeeProperty(room, "valve_position", 0);
            //    device.SetDeviceAndZigbeeProperty(room, "system_mode", "off");
            //}
            //else
            //{
            //    _ = room.Zigbee2Mqtt.SetValue(device.FriendlyName, "eurotronic_host_flags", JToken.FromObject(new { window_open = false }));
            //    device.SetDeviceAndZigbeeProperty(room, "trv_mode", 2);
            //    device.SetDeviceAndZigbeeProperty(room, "system_mode", "auto");
            //}
        }
    }

    private static IEnumerable<Device> FilterDevices(IEnumerable<Device> devices, Func<Device, bool> filter)
    {
        foreach (var device in devices)
        {
            if (filter(device))
                yield return device;
        }
    }

    private static bool IsHeater(Device device)
        => device.TypeName is "SPZB0001";

    private static bool IsContactGroup(Device device)
        => device.TypeName is "contact_group";

    private static bool IsAverageTemperature(Device device)
        => device.TypeName is "average_temperature";

    private static bool IsRoomOpen(IEnumerable<Device> devices)
        => FilterDevices(devices, IsContactGroup)
        .OfType<GroupingDevice<byte>>()
        .Min(device => device.Value) < 1;

    private static IHeaterConfigModel? GetCurrentHeaterConfig(long deviceId)
    {
        using BrokerDbContext? cont = DbProvider.BrokerDbContext;
        DeviceModel? d = cont.Devices
            .Include(x => x.HeaterConfigs)
            .FirstOrDefault(x => x.Id == deviceId);

        if (d is null || d.HeaterConfigs is null || d.HeaterConfigs.Count < 1)
            return null;

        IHeaterConfigModel? bestFit = null;

        var curDow = (DayOfWeek)((int)(DateTime.Now.DayOfWeek + 6) % 7);
        var curTimeOfDay = DateTime.Now.TimeOfDay;
        foreach (var item in d.HeaterConfigs.OrderByDescending(x => x.DayOfWeek).ThenByDescending(x => x.TimeOfDay))
        {
            bestFit = item;

            if ((item.DayOfWeek == curDow
                    && item.TimeOfDay.TimeOfDay < curTimeOfDay)
                || item.DayOfWeek < curDow)
            {
                break;
            }
        }
        return bestFit;
    }

    private static float GetLocalCalibration(IDeviceStateManager stateManager, Device device)
        => stateManager.GetSingleState(device.Id, "local_temperature_calibration")?.ToObject<float>() ?? 0;

    private static float GetLocalTemperature(IDeviceStateManager stateManager, Device device)
        => stateManager.GetSingleState(device.Id, "local_temperature")?.ToObject<float>() ?? 0;

    private static void SetDeviceAndZigbeeProperty(this Device device, Room room, string name, JToken newValue)
    {
        _ = room.Zigbee2Mqtt.SetValue(device.FriendlyName, name, newValue);
        room.StateManager.PushNewState(device.Id, name, newValue);
    }

    private static string ToZigbeeId(this long id)
        => $"0x{id:x16}";

    private static string ToZigbeeId(this Device device)
        => ToZigbeeId(device.Id);
}

public record struct Room(
    string Name,
    Device MainDevice,
    IReadOnlyList<Device> Devices,
    IDeviceStateManager StateManager,
    Zigbee2MqttManager Zigbee2Mqtt,
    double CurrentTemperature,
    bool IsOpen,
    IHeaterConfigModel? CurrentPlan
);