using AppBroker.Core.Devices;
using AppBroker.Core;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using AppBroker.Core.Database;

namespace AppBroker.App.Hubs;
public class DeviceHub
{

    public static List<Device> GetAllDevices()
    {
        var devices = IInstanceContainer.Instance.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();
        var dev = JsonConvert.SerializeObject(devices);

        return devices;
    }

    public record struct DeviceOverview(long Id, string TypeName, IReadOnlyCollection<string> TypeNames, string FriendlyName);
    public static List<DeviceOverview> GetDeviceOverview() => IInstanceContainer.Instance.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).Select(x => new DeviceOverview(x.Id, x.TypeName, x.TypeNames, x.FriendlyName)).ToList();

    public static void UpdateDevice(long id, string newName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? stored))
        {
            stored.FriendlyName = newName;
            _ = DbProvider.UpdateDeviceInDb(stored);
            stored.SendDataToAllSubscribers();
        }

    }


    public static List<Device> Subscribe(DynamicHub hub, List<long> DeviceIds)
    {
        string connectionId = hub.Context.ConnectionId;
        var devices = new List<Device>();
        string? subMessage = "User subscribed to ";
        foreach (long deviceId in DeviceIds)
        {

            if (!IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Device? device))
                continue;


            if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                device.Subscribers.Add(new Subscriber(connectionId, hub.Clients.Caller));
            devices.Add(device);
            subMessage += device.Id + "/" + device.FriendlyName + ", ";
        }
        Console.WriteLine(subMessage);
        var dev = JsonConvert.SerializeObject(devices);

        return devices;
    }

    public static void Unsubscribe(DynamicHub hub, List<long> DeviceIds)
    {
        string connectionId = hub.Context.ConnectionId;
        var devices = new List<Device>();
        string? subMessage = "User unsubscribed from ";
        foreach (long deviceId in DeviceIds)
        {
            if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Device? device))
            {

                _ = device.Subscribers.RemoveWhere(x => x.ConnectionId == connectionId);
                subMessage += device.Id + "/" + device.FriendlyName + ", ";
            }
        }
        Console.WriteLine(subMessage);
    }

}
