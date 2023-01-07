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

namespace AppBroker.maxi;

[Variant]
internal partial struct RoomStateChange
{
    static partial void VariantOf(float temperature, bool isOpen, IHeaterConfigModel heaterConfigModel);
}

internal static class RoomFactory
{

    public static IObservable<Room> Create(string name, IDeviceStateManager stateManager, Zigbee2MqttManager zigbee2Mqtt, Device mainDevice, params Device[] devices)
    {
        if (!devices.Contains(mainDevice))
        {
            var oldDevices = devices;
            devices = new Device[oldDevices.Length];
            Array.Copy(oldDevices, 0, devices, 0, oldDevices.Length);
            devices[^1] = mainDevice;
        }

        var currentConfig = GetCurrentHeaterConfig(mainDevice.Id);

        var room = new Room(name, mainDevice, devices, stateManager, zigbee2Mqtt, 0.0f, false, currentConfig);

        var changesByTime
                    = Observable
                    .Interval(TimeSpan.FromMinutes(1))
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
            .Select(eventPattern => new RoomStateChange(eventPattern.EventArgs < 1));

        var stateChanges
            = Observable
            .Merge(changesByTime, temperatureChanges, contactChanges);

        return stateChanges
            .Scan(room, (room, change)
                => change
                    .Visit(
                        (float average) =>
                            {
                                if (room.CurrentTemperature == average)
                                    return room;

                                var newRoom = room with { CurrentTemperature = average };
                                CalibrateTemperature(newRoom);
                                return newRoom;
                            },
                        (bool isOpen) =>
                            {
                                if (room.IsOpen == isOpen)
                                    return room;

                                var newRoom = room with { IsOpen = isOpen };
                                UpdateRoomHeaterOpenWindow(newRoom);
                                return newRoom;
                            },
                        (IHeaterConfigModel model) =>
                            {
                                if (room.CurrentPlan == model)
                                    return room;

                                var newRoom = room with { CurrentPlan = model };
                                SetTargetTemperature(newRoom);
                                return newRoom;
                            }
                     )
            )
            .DistinctUntilChanged();
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
        FilterDevices(room.Devices, IsHeater)
            .OfType<Zigbee2MqttDevice>()
            .Select(device => (device, localTemperature: GetLocalTemperature(room.StateManager, device)))
            .ToList()
            .ForEach(temperatureInfo =>
            {
                var offset = room.CurrentTemperature - temperatureInfo.localTemperature;
                _ = room.Zigbee2Mqtt.SetValue(temperatureInfo.device.FriendlyName, "local_temperature_calibration", offset);
            });
    }

    private static void SetTargetTemperature(Room room)
    {
        if (room.IsOpen)
            return;

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

        foreach (var device in climates)
        {
            if (room.IsOpen)
            {
                _ = room.Zigbee2Mqtt.SetValue(device.FriendlyName, "eurotronic_host_flags", JToken.FromObject(new { window_open = true }));
                device.SetDeviceAndZigbeeProperty(room, "trv_mode", 1);
                device.SetDeviceAndZigbeeProperty(room, "valve_position", 0);
                device.SetDeviceAndZigbeeProperty(room, "system_mode", "off");
            }
            else
            {
                _ = room.Zigbee2Mqtt.SetValue(device.FriendlyName, "eurotronic_host_flags", JToken.FromObject(new { window_open = false }));
                device.SetDeviceAndZigbeeProperty(room, "trv_mode", 2);
                device.SetDeviceAndZigbeeProperty(room, "system_mode", "auto");
            }
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
    float CurrentTemperature,
    bool IsOpen,
    IHeaterConfigModel? CurrentPlan
);