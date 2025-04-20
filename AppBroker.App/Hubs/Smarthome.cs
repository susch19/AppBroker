using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;
using AppBroker.Core.Models;

using AppBrokerASP;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

namespace AppBroker.App.Hubs;

public static class SmartHome
{
    [Obsolete("Use REST Method instead")]
    public static async Task Update(JsonSmarthomeMessage message)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(message.NodeId, out Device? device))
        {
            switch (message.MessageType)
            {
                case MessageType.Get:
                    break;
                case MessageType.Update:
                    await device.UpdateFromApp(message.Command, message.Parameters);
                    break;
                case MessageType.Options:
                    device.OptionsFromApp(message.Command, message.Parameters);
                    break;
                default:
                    break;
            }
            //Console.WriteLine($"User send command {message.Command} to {device} with {message.Parameters}");
        }
    }

    [Obsolete("Use REST Method instead")]
    public static dynamic? GetConfig(long deviceId) => IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Device? device) ? device.GetConfig() : null;

    public static async void SendUpdate(DynamicHub hub, Device device) => await (hub.Clients.All?.Update(device) ?? Task.CompletedTask);
}
