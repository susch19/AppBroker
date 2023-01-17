using AppBroker.Core;
using AppBroker.Core.Extension;
using AppBroker.maxi.Devices;

using NLog;
using Microsoft.EntityFrameworkCore;
using AppBroker.Core.Managers;
using AppBroker.Core.Devices;
using AppBroker.Zigbee2Mqtt;
using AppBrokerASP;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using MQTTnet.Client;
using System.Reactive.Subjects;
using System.Linq;

namespace AppBroker.maxi;

internal class Plugin : IPlugin
{
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

    public string Name => "Maxi Private Plugin";
    private readonly SerialDisposable serialDisposable;
    private Logger _mainLogger;

    public Plugin()
    {
        serialDisposable = new SerialDisposable();
        _mainLogger = LogManager.CreateNullLogger();
    }

    public bool Initialize(LogFactory logFactory)
    {
        static IReadOnlyCollection<long> GetDevices(IReadOnlyDictionary<long, Device> devices) 
            => devices
                .Select(valuePair => valuePair.Key)
                .ToList();

        _mainLogger = LogManager.GetLogger(Name);
        _mainLogger.Info("Start to initalize plugin");

        var livingRoomDevices = new[] { 0x00158d0002389da1, 0x00158d00053d2ba1, 0x00158d0002ab36dd, 0x00158d00054d9840, 0x00158d0002a1d9c2 };
        var kitchenDevices = new[] { 0x00158d000238a35e, 0x00158d00053d2bb8, 0x00158d0002a1e224 };
        var bathDevices = new[] { 0x00158d0002775c7e, 0x00158d0001ff8e1e, 0x00158d0002a1e1a8 };
        var bedroomDevices = new[] { 0x00158d000349bf07, 0x00158d00053d2b33, 0x00158d0002ab319a };
        var corridorDevices = new[] { 0x00158d000349c41a, 0x00158d00054d9f9e };

        var zigbeeManager = InstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>();
        var deviceManager = IInstanceContainer.Instance.DeviceManager;
        var initDevices = GetDevices(deviceManager.Devices);

        serialDisposable
            .Disposable
                = Observable
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
                        = livingRoomDevices
                        .Concat(kitchenDevices)
                        .Concat(bathDevices)
                        .Concat(bedroomDevices)
                        .Concat(corridorDevices)
                        .Memoize();

                    var exceptDevices = allDevices.Except(rcvDeviceIds);


                    if (exceptDevices.Any())
                    { 
                        _mainLogger.Error($"Can not create rooms, because ids are missing in device manager: {exceptDevices}");
                        return Observable.Empty<Room>();
                    }

                    _mainLogger.Info("New Devices added start to generate rooms");

                    var stateManager = IInstanceContainer.Instance.DeviceStateManager;

                    var livingroom = CreateRoom(livingRoomId, "livingroom", deviceManager, stateManager, zigbeeManager, _mainLogger, livingRoomDevices);
                    var kitchen = CreateRoom(kitchenRoomId, "kitchen", deviceManager, stateManager, zigbeeManager, _mainLogger, kitchenDevices);
                    var bath = CreateRoom(bathRoomId, "bath", deviceManager, stateManager, zigbeeManager, _mainLogger, bathDevices);
                    var bedroom = CreateRoom(bedRoomId, "bedroom", deviceManager, stateManager, zigbeeManager, _mainLogger, bedroomDevices);
                    var corridor = CreateRoom(corridorRoomId, "corridor", deviceManager, stateManager, zigbeeManager, _mainLogger, corridorDevices);

                    var stopRooms
                        = Disposable
                        .Create(
                            () => _mainLogger.Info("Stop to subscribe on rooms")
                        );

                    var rooms = Observable.Merge(livingroom, kitchen, bath, bedroom, corridor);

                    return Observable
                        .Using(
                            () =>
                            {
                                _mainLogger.Info("Start to subscribe on rooms");
                                return stopRooms;
                            },
                            _ => rooms
                        );
                })
                .Serial()
                .Subscribe();

        _mainLogger.Info("Initialize successfull");

        return true;
    }

    private static IObservable<Room> CreateRoom(long roomId, string roomName, IDeviceManager deviceManager, IDeviceStateManager stateManager, Zigbee2MqttManager zigbee2Mqtt, ILogger logger, params long[] deviceIds)
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
        //var devices = wheater.ToArray();

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

public static class IObservableExtension
{
    public static IObservable<T> Serial<T>(this IObservable<IObservable<T>> observable)
    {
        return Observable
            .Using(
                () => new SerialContext<T>(observable),
                context => context.Select()
            );
    }


    private class SerialContext<T> : IDisposable
    {
        public Subject<T> Subject { get; }
        public SerialDisposable SerialSubscription { get; }

        private IDisposable? internalSub;

        private readonly IObservable<IObservable<T>> internalStream;

        public SerialContext(IObservable<IObservable<T>> serialStream)
        {
            Subject = new Subject<T>();
            SerialSubscription = new SerialDisposable();

            internalStream = serialStream;
        }

        public IObservable<T> Select()
        {
            internalSub
                = internalStream
                .Subscribe(
                    serialEelement => SerialSubscription.Disposable = serialEelement.Subscribe(Subject),
                    Subject.OnError,
                    Subject.OnCompleted
                );

            return Subject;
        }


        public void Dispose()
        {
            SerialSubscription.Dispose();
            Subject.Dispose();
            internalSub?.Dispose();
        }
    }
}

