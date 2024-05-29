using AppBroker.Core;
using AppBroker.Notifications.Configuration;

using FirebaseAdmin.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Notifications.Hubs;
public class NotificationHub
{
    public static Dictionary<string, FirebaseOptions>? GetFirebaseOptions()
    {
        return IInstanceContainer.Instance.ConfigManager.PluginConfigs.OfType<FirebaseConfig>().FirstOrDefault()?.Options;
    }

}


