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

    public Plugin()
    {
        serialDisposable = new SerialDisposable();
    }

    public bool Initialize(LogFactory logFactory)
    {
        var zigbeeManager = InstanceContainer.Instance.GetDynamic<Zigbee2MqttManager>();

        serialDisposable
            .Disposable
                = Observable
                .FromEventPattern(
                    add => zigbeeManager.DevicesReceived += add,
                    remove => zigbeeManager.DevicesReceived += remove
                )
                .Select(_ =>
                {
                    var deviceManager = IInstanceContainer.Instance.DeviceManager;
                    var stateManager = IInstanceContainer.Instance.DeviceStateManager;

                    var livingroom = CreateRoom(livingRoomId, "livingroom", deviceManager, stateManager, zigbeeManager, 0x00158d0002389da1, 0x00158d00053d2ba1, 0x00158d0002ab36dd, 0x00158d00054d9840, 0x00158d0002a1d9c2);
                    var kitchen = CreateRoom(kitchenRoomId, "kitchen", deviceManager, stateManager, zigbeeManager, 0x00158d000238a35e, 0x00158d00053d2bb8, 0x00158d0002a1e224);
                    var bath = CreateRoom(bathRoomId, "bath", deviceManager, stateManager, zigbeeManager, 0x00158d0002775c7e, 0x00158d0001ff8e1e, 0x00158d0002a1e1a8);
                    var bedroom = CreateRoom(bedRoomId, "bedroom", deviceManager, stateManager, zigbeeManager, 0x00158d000349bf07, 0x00158d00053d2b33, 0x00158d0002ab319a);
                    var corridor = CreateRoom(corridorRoomId, "corridor", deviceManager, stateManager, zigbeeManager, 0x00158d000349c41a, 0x00158d00054d9f9e);

                    return Observable.Merge(livingroom, kitchen, bath, bedroom, corridor);
                })
                .Concat()
                .Subscribe();

        return true;
    }

    private static IObservable<Room> CreateRoom(long roomId, string roomName, IDeviceManager deviceManager, IDeviceStateManager stateManager, Zigbee2MqttManager zigbee2Mqtt, params long[] deviceIds)
    {
        var mainDevice = new RoomDevice(virtualDeviceBase + roomId + mainDeviceId);
        var averageGroup = CreateAverageGroup(deviceManager, deviceIds, roomName, roomId);
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

        return RoomFactory.Create(roomName, stateManager, zigbee2Mqtt, mainDevice, roomDevices);
    }

    private static GroupingDevice<float> CreateAverageGroup(IDeviceManager manager, IEnumerable<long> ids, string roomName, long roomId)
    {
        var heater = FilterDevices(manager, ids, IsHeater).Select(device => ("local_temperature", device.Id));
        var wheater = FilterDevices(manager, ids, IsWheater).Select(device => ("temperature", device.Id));

        return new GroupingDevice<float>(
                virtualDeviceBase + roomId + averageDeviceId,
                GroupingMode.Avg,
                "temperature",
                heater.Concat(wheater).ToArray()
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
