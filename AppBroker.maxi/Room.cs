using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Models;
using AppBroker.Zigbee2Mqtt;

namespace AppBroker.maxi;

public record struct Room(
    string Name,
    Device MainDevice,
    IReadOnlyList<Device> Devices,
    IDeviceStateManager StateManager,
    IZigbee2MqttManager Zigbee2Mqtt,
    double CurrentTemperature,
    bool IsOpen,
    IHeaterConfigModel? CurrentPlan
);