using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Managers;
using AppBroker.maxi.Devices;
using AppBroker.Zigbee2Mqtt;

using Microsoft.EntityFrameworkCore;

using NLog;

using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace AppBroker.maxi;

public static class PluginFactory
{
    public readonly record struct DeviceContext(IReadOnlyCollection<long> LivingRoomDevices, IReadOnlyCollection<long> KitchenDevices, IReadOnlyCollection<long> BathDevices, IReadOnlyCollection<long> BedroomDevices, IReadOnlyCollection<long> CorridorDevices);

    private const long virtualDeviceBase = 0x1100000000000000;
    private const long roomOffset = 0x0000000000000100;
    private const long mainDeviceId = 0x0000000000000000;
    private const long averageDeviceId = 0x0000000000000001;
    private const long conactGroupDeviceId = 0x0000000000000002;

    private const long livingRoomId = 1 * roomOffset;
    private const long kitchenRoomId = 2 * roomOffset;
    private const long bathRoomId = 3 * roomOffset;
    private const long bedRoomId = 4 * roomOffset;
    private const long corridorRoomId = 5 * roomOffset;

    public static IObservable<Room> Create(ILogger logger, IZigbee2MqttManager zigbeeManager, IDeviceManager deviceManager, DeviceContext context)
    {
        var initDevices = GetDevices(deviceManager.Devices);

        static IReadOnlyCollection<long> GetDevices(IReadOnlyDictionary<long, Device> devices)
            => devices
                .Select(valuePair => valuePair.Key)
                .ToList();

        return Observable
                 .FromEventPattern(
                     add => zigbeeManager.DevicesReceived += add,
                     remove => zigbeeManager.DevicesReceived += remove
                 )
                 .Scan(initDevices, (currentDeviceIds, newDevices) =>
                 {
                     var newDeviceIds = GetDevices(deviceManager.Devices);

                     if (newDeviceIds.Any(id => !currentDeviceIds.Contains(id)))
                         return newDeviceIds;

                     return currentDeviceIds;
                 })
                 .DistinctUntilChanged()
                 .Select(rcvDeviceIds =>
                 {
                     using var allDevices
                         = context.LivingRoomDevices
                         .Concat(context.KitchenDevices)
                         .Concat(context.BathDevices)
                         .Concat(context.BedroomDevices)
                         .Concat(context.CorridorDevices)
                         .Memoize();

                     var exceptDevices = allDevices.Except(rcvDeviceIds);


                     if (exceptDevices.Any())
                     {
                         logger.Error($"Can not create rooms, because ids are missing in device manager: {exceptDevices}");
                         return Observable.Empty<Room>();
                     }

                     logger.Info("New Devices added start to generate rooms");

                     var stateManager = IInstanceContainer.Instance.DeviceStateManager;

                     var livingroom = CreateRoom(livingRoomId, "livingroom", deviceManager, stateManager, zigbeeManager, logger, context.LivingRoomDevices);
                     var kitchen = CreateRoom(kitchenRoomId, "kitchen", deviceManager, stateManager, zigbeeManager, logger, context.KitchenDevices);
                     var bath = CreateRoom(bathRoomId, "bath", deviceManager, stateManager, zigbeeManager, logger, context.BathDevices);
                     var bedroom = CreateRoom(bedRoomId, "bedroom", deviceManager, stateManager, zigbeeManager, logger, context.BedroomDevices);
                     var corridor = CreateRoom(corridorRoomId, "corridor", deviceManager, stateManager, zigbeeManager, logger, context.CorridorDevices);

                     var stopRooms
                         = Disposable
                         .Create(
                             () => logger.Info("Stop to subscribe on rooms")
                         );

                     var rooms = Observable.Merge(livingroom, kitchen, bath, bedroom, corridor);

                     return Observable
                         .Using(
                             () =>
                             {
                                 logger.Info("Start to subscribe on rooms");
                                 return stopRooms;
                             },
                             _ => rooms
                         );
                 })
                 .Serial();
    }

    private static IObservable<Room> CreateRoom(
        long roomId,
        string roomName,
        IDeviceManager deviceManager,
        IDeviceStateManager stateManager,
        IZigbee2MqttManager zigbee2Mqtt,
        ILogger logger,
        IReadOnlyCollection<long> deviceIds
        )
    {
        var roomLogger = logger.Factory.GetLogger($"{logger.Name}.{roomName}");

        logger.Debug($"creating {roomName} mainDevice");
        var mainDevice = new RoomDevice(virtualDeviceBase + roomId + mainDeviceId, roomName);

        logger.Debug($"creating {roomName} average group");
        var averageGroup = CreateAverageGroup(deviceManager, deviceIds, roomName, roomId);

        logger.Debug($"creating {roomName} contact group");
        var contactGroup = CreateContactGroup(deviceManager, deviceIds, roomName, roomId);

        var roomDevices
            = deviceIds
            .Select(id => deviceManager.Devices[id])
            .Prepend(mainDevice)
            .Append(averageGroup)
            .Append(contactGroup)
            .ToArray();

        deviceManager.AddNewDevice(mainDevice);
        deviceManager.AddNewDevice(averageGroup);
        deviceManager.AddNewDevice(contactGroup);

        logger.Debug($"creating {roomDevices.Length} devices for {roomName}");

        return RoomFactory.Create(roomName, roomLogger, stateManager, zigbee2Mqtt, mainDevice, roomDevices);
    }

    private static GroupingDevice<float> CreateAverageGroup(IDeviceManager manager, IEnumerable<long> ids, string roomName, long roomId)
    {
        var heater = FilterDevices(manager, ids, IsHeater).Select(device => ("unoccupied_local_temperature", device.Id));
        var wheater = FilterDevices(manager, ids, IsWheater).Select(device => ("temperature", device.Id));

        var devices = heater.Concat(wheater).ToArray();

        return new GroupingDevice<float>(
                virtualDeviceBase + roomId + averageDeviceId,
                GroupingMode.Avg,
                "temperature",
                0,
                devices
            )
        {
            FriendlyName = $"{roomName}_average_temperature",
            TypeName = "average_temperature"
        };
    }

    private static GroupingDevice<byte> CreateContactGroup(IDeviceManager manager, IEnumerable<long> ids, string roomName, long roomId)
    {
        return new GroupingDevice<byte>(
                virtualDeviceBase + roomId + conactGroupDeviceId,
                GroupingMode.Min,
                "contact",
                0,
                FilterDevices(manager, ids, IsContactSensor).Select(device => device.Id).ToArray()
            )
        {
            FriendlyName = $"{roomName}_contact_group",
            TypeName = "contact_group"
        };
    }

    private static IEnumerable<Device> FilterDevices(IDeviceManager manager, IEnumerable<long> ids, Func<Device, bool> filter)
    {
        foreach (var deviceId in ids)
        {
            var device = manager.Devices[deviceId];

            if (filter(device))
                yield return device;
        }
    }

    private static bool IsContactSensor(Device device)
        => device.TypeName == "MCCGQ11LM";

    private static bool IsHeater(Device device)
        => device.TypeName is "SPZB0001";

    private static bool IsWheater(Device device)
        => device.TypeName is "WSDCGQ11LM";
}