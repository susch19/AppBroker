using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Core.Devices;
using AppBroker.Core.DynamicUI;
using AppBroker.Core.Models;

using AppBrokerASP.Plugins;

using Microsoft.AspNetCore.SignalR;

using Newtonsoft.Json;

using NLog.Targets;

using System.Reflection;
using System.Threading.Tasks;

namespace AppBrokerASP;

public class SmartHome : Hub<ISmartHomeClient>
{
    public SmartHome()
    {
    }



    public override Task OnConnectedAsync() =>
        //foreach (var item in IInstanceContainer.Instance.DeviceManager.Devices.Values)
        //    item.SendLastData(Clients.Caller);

        base.OnConnectedAsync();

    public async Task Update(JsonSmarthomeMessage message)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(message.LongNodeId, out Device? device))
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
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? stored))
        {
            stored.FriendlyName = newName;
            _ = DbProvider.UpdateDeviceInDb(stored);
            stored.SendDataToAllSubscribers();
        }

    }

    public dynamic? GetConfig(long deviceId) => IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Device? device) ? device.GetConfig() : null;

    public async void SendUpdate(Device device) => await (Clients.All?.Update(device) ?? Task.CompletedTask);

    public List<Device> GetAllDevices()
    {
        var devices = IInstanceContainer.Instance.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).ToList();
        var dev = JsonConvert.SerializeObject(devices);

        return devices;
    }

    public List<HistoryPropertyState> GetHistoryPropertySettings() => IInstanceContainer.Instance.HistoryManager.GetHistoryProperties();
    public void SetHistory(bool enable, long id, string name)
    {
        if (enable)
            IInstanceContainer.Instance.HistoryManager.EnableHistory(id, name);
        else
            IInstanceContainer.Instance.HistoryManager.DisableHistory(id, name);
    }
    public void SetHistories(bool enable, List<long> ids, string name)
    {
        if (enable)
        {
            foreach (var id in ids)
                IInstanceContainer.Instance.HistoryManager.EnableHistory(id, name);
        }
        else
        {
            foreach (var id in ids)
                IInstanceContainer.Instance.HistoryManager.DisableHistory(id, name);
        }
    }

    public record struct DeviceOverview(long Id, string TypeName, IReadOnlyCollection<string> TypeNames, string FriendlyName);
    public List<DeviceOverview> GetDeviceOverview() => IInstanceContainer.Instance.DeviceManager.Devices.Select(x => x.Value).Where(x => x.ShowInApp).Select(x => new DeviceOverview(x.Id, x.TypeName, x.TypeNames, x.FriendlyName)).ToList();


    public Task<List<History>> GetIoBrokerHistories(long id, string dt)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            DateTime date = DateTime.Parse(dt).Date;
            return device.GetHistory(date, date.AddDays(1).AddSeconds(-1));
        }
        return Task.FromResult(new List<History>());
    }

    public virtual Task<History> GetIoBrokerHistory(long id, string dt, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            DateTime date = DateTime.Parse(dt).Date;
            return device.GetHistory(date, date.AddDays(1).AddSeconds(-1), propertyName);
        }
        return Task.FromResult(History.Empty);
    }

    public Task<List<History>> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return device.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2));
        }

        return Task.FromResult(new List<History>());
    }

    public virtual async Task<History> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return await device.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2), propertyName)
                ;
        }

        return History.Empty;
    }


    public List<Device> Subscribe(List<long> DeviceIds)
    {
        string connectionId = Context.ConnectionId;
        var devices = new List<Device>();
        string? subMessage = "User subscribed to ";
        foreach (long deviceId in DeviceIds)
        {

            if (!IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(deviceId, out Device? device))
                continue;


            if (!device.Subscribers.Any(x => x.ConnectionId == connectionId))
                device.Subscribers.Add(new Subscriber(connectionId, Clients.Caller));
            devices.Add(device);
            subMessage += device.Id + "/" + device.FriendlyName + ", ";
        }
        Console.WriteLine(subMessage);
        var dev = JsonConvert.SerializeObject(devices);

        return devices;
    }

    public void Unsubscribe(List<long> DeviceIds)
    {
        string connectionId = Context.ConnectionId;
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

    public void UpdateTime()
    {
        //TODO How to call smarthome mesh manager update time, without knowing the existence of said manager?
    }

    public string GetHashCodeByTypeName(string typeName) => InstanceContainer.Instance.IconService.GetBestFitIcon(typeName).Hash;
    public string GetHashCodeByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName).Hash;

    public SvgIcon GetIconByTypeName(string typename) => InstanceContainer.Instance.IconService.GetBestFitIcon(typename);
    public SvgIcon GetIconByName(string iconName) => InstanceContainer.Instance.IconService.GetIconByName(iconName);
    public SvgIcon GetIconByDeviceId(long deviceId) => InstanceContainer.Instance.IconService.GetBestFitIcon(InstanceContainer.Instance.DeviceManager.Devices[deviceId].TypeName);

    public void ReloadDeviceLayouts() => DeviceLayoutService.ReloadLayouts();
    public DeviceLayout? GetDeviceLayoutByName(string typename) => DeviceLayoutService.GetDeviceLayout(typename)?.layout;
    public DeviceLayout? GetDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDeviceLayout(id)?.layout;
    public List<DeviceLayout> GetAllDeviceLayouts() => DeviceLayoutService.GetAllLayouts();

    public record LayoutNameWithHash(string Name, string Hash);
    public LayoutNameWithHash? GetDeviceLayoutHashByDeviceId(long id)
    {

        var layoutHash = DeviceLayoutService.GetDeviceLayout(id);
        if (layoutHash is null || layoutHash.Value.layout is null)
            return null;

        return new(layoutHash.Value.layout.UniqueName, layoutHash.Value.hash);
    }

    public IEnumerable<KeyValuePair<string, string>> GetAppConfig()
    {
        var loader = IInstanceContainer.Instance.GetDynamic<PluginLoader>();
        return loader.AppConfigurators.Select(x => x.GetConfigs()).Where(x => x is not null).SelectMany(x => x!);
    }



    //public DashboardDeviceLayout? GetDashboardDeviceLayoutByName(string typename) => DeviceLayoutService.GetDashboardDeviceLayout(typename);
    //public DashboardDeviceLayout? GetDashboardDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDashboardDeviceLayout(id);
    //public DetailDeviceLayout? GetDetailDeviceLayoutByName(string typename) => DeviceLayoutService.GetDetailDeviceLayout(typename);
    //public DetailDeviceLayout? GetDetailDeviceLayoutByDeviceId(long id) => DeviceLayoutService.GetDetailDeviceLayout(id);
}
