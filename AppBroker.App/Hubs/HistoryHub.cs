using AppBroker.Core.Devices;
using AppBroker.Core.Models;
using AppBroker.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.App.Hubs;
public class HistoryHub
{

    public static List<HistoryPropertyState> GetHistoryPropertySettings() => IInstanceContainer.Instance.HistoryManager.GetHistoryProperties();
    public static void SetHistory(bool enable, long id, string name)
    {
        if (enable)
            IInstanceContainer.Instance.HistoryManager.EnableHistory(id, name);
        else
            IInstanceContainer.Instance.HistoryManager.DisableHistory(id, name);
    }
    public static void SetHistories(bool enable, List<long> ids, string name)
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

    public static Task<List<History>> GetIoBrokerHistories(long id, string dt)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            DateTime date = DateTime.Parse(dt).Date;
            return device.GetHistory(date, date.AddDays(1).AddSeconds(-1));
        }
        return Task.FromResult(new List<History>());
    }

    public static Task<History> GetIoBrokerHistory(long id, string dt, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            DateTime date = DateTime.Parse(dt).Date;
            return device.GetHistory(date, date.AddDays(1).AddSeconds(-1), propertyName);
        }
        return Task.FromResult(History.Empty);
    }

    public static Task<List<History>> GetIoBrokerHistoriesRange(long id, string dt, string dt2)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return device.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2));
        }

        return Task.FromResult(new List<History>());
    }

    public static async Task<History> GetIoBrokerHistoryRange(long id, string dt, string dt2, string propertyName)
    {
        if (IInstanceContainer.Instance.DeviceManager.Devices.TryGetValue(id, out Device? device))
        {
            return await device.GetHistory(DateTime.Parse(dt), DateTime.Parse(dt2), propertyName)
                ;
        }

        return History.Empty;
    }

}
