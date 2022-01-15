using System.Reflection;
using System.Threading.Tasks;

using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;

using AppBrokerASP.Database;
using AppBrokerASP.Devices;
using AppBrokerASP.Devices.Zigbee;
using AppBrokerASP.IOBroker;

using Microsoft.AspNetCore.SignalR;

using PainlessMesh;

namespace AppBrokerASP;

public class SmartHome : Hub<ISmartHomeClient>
{
    public SmartHome()
    {
    }

    public override Task OnConnectedAsync()
    {
        //foreach (var item in IInstanceContainer.Instance.DeviceManager.Devices.Values)
        //    item.SendLastData(Clients.Caller);

        return base.OnConnectedAsync();
    }

    public async Task Update(JsonSmarthomeMessage message)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(message.LongNodeId, out var device))
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

    public void UpdateDevice(long id, string newName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var stored))
        {
            stored.FriendlyName = newName;
            _ = DbProvider.UpdateDeviceInDb(stored);
            stored.SendDataToAllSubscribers();
        }
    }

    public dynamic? GetConfig(uint deviceId) => IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device) ? device.GetConfig() : null;

    public async void SendUpdate(Device device) => await (Clients.All?.Update(device) ?? Task.CompletedTask);

    public List<Device> GetAllDevices() => IInstanceContainer.Instance.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();

    public Task<List<IoBrokerHistory>> GetIoBrokerHistories(long id, string dt)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
        {
            var date = DateTime.Parse(dt).Date;
            return d.GetHistory(date, date.AddDays(1).AddSeconds(-1));
        }
        return Task.FromResult(new List<IoBrokerHistory>());
    }

    public Task<IoBrokerHistory> GetIoBrokerHistory(long id, string dt, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
        {
            var date = DateTime.Parse(dt).Date;
            return d.GetHistory(date, date.AddDays(1).AddSeconds(-1), Enum.Parse<HistoryType>(propertyName, true));
        }
        return Task.FromResult(new IoBrokerHistory());
    }

    public Task<List<IoBrokerHistory>> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
        {
            return d.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2));
        }

        return Task.FromResult(new List<IoBrokerHistory>());
    }

    // TODO: remove list, just return one item
    public async Task<List<IoBrokerHistory>> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out var device) && device is ZigbeeDevice d)
        {
            return new List<IoBrokerHistory>()
                {
                    await d.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2), Enum.Parse<HistoryType>(propertyName, true))
                };
        }

        return new List<IoBrokerHistory>();
    }

    public List<Device> Subscribe(IEnumerable<long> DeviceIds)
    {
        var proxyFieldInfo = Clients
            .Caller
            .GetType()
            .GetRuntimeFields()
            .First(x => x.Name == "_proxy");
        var proxy = proxyFieldInfo!.GetValue(Clients.Caller)!;
        var highlightedItemProperty =
            proxy
            .GetType()
            .GetRuntimeFields()
            .First(pi => pi.Name == "_connectionId");
        string connectionId = (string)highlightedItemProperty.GetValue(proxy)!;
        var devices = new List<Device>();
        var subMessage = "User subscribed to ";
        foreach (var deviceId in DeviceIds)
        {

            if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out var device))
            {
                if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                    device.Subscribers.Add(new Subscriber(connectionId, Clients.Caller));
                devices.Add(device);
                subMessage += device.Id + "/" + device.FriendlyName + ", ";
            }
        }
        Console.WriteLine(subMessage);
        return devices;
    }

    public void UpdateTime() => InstanceContainer.Instance.MeshManager.UpdateTime();

    public byte[] GetIconByTypeName(string typename) => InstanceContainer.Instance.IconService.GetBestFitIcon(typename);
    public byte[] GetIconByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName);

    public byte[] GetIconByDeviceId(long deviceId) => InstanceContainer.Instance.IconService.GetBestFitIcon(InstanceContainer.Instance.DeviceManager.Devices[deviceId].TypeName);

    public void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
    public DeviceLayout? GetDeviceLayoutByName(string typename) => DeviceLayoutService.GetDeviceLayout(typename);
    public DeviceLayout? GetDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDeviceLayout(id);
    public DashboardDeviceLayout? GetDashboardDeviceLayoutByName(string typename) => DeviceLayoutService.GetDashboardDeviceLayout(typename);
    public DashboardDeviceLayout? GetDashboardDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDashboardDeviceLayout(id);
    public DetailDeviceLayout? GetDetailDeviceLayoutByName(string typename) => DeviceLayoutService.GetDetailDeviceLayout(typename);
    public DetailDeviceLayout? GetDetailDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDetailDeviceLayout(id);
}