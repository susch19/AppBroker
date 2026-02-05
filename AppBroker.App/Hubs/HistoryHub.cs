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
    [Obsolete("Use REST Method instead")]
    public static void SetHistory(bool enable, long id, string name)
    {
        if (enable)
            IInstanceContainer.Instance.HistoryManager.EnableHistory(id, name);
        else
            IInstanceContainer.Instance.HistoryManager.DisableHistory(id, name);
    }

    [Obsolete("Use REST Method instead")]
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
}
