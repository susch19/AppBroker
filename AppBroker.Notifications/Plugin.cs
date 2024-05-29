using AppBroker.Core;
using AppBroker.Core.Devices;
using AppBroker.Core.Extension;
using AppBroker.Notifications.Hubs;

using AppBrokerASP;
using AppBrokerASP.Extension;

using FirebaseAdmin;
using FirebaseAdmin.Messaging;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

using NLog;

using System.Runtime.InteropServices;

namespace AppBroker.Notifications;

internal class Plugin : IPlugin
{
    private Logger logger;
    private Dictionary<long, Timer> lastReceivedTimer = new Dictionary<long, Timer>();

    public string Name => "Notifications";
     

    public bool Initialize(LogFactory logFactory)
    {
        logger = logFactory.GetCurrentClassLogger();

        var app = FirebaseApp.Create(new AppOptions { Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile("service_account.json") });
        //FirebaseMessaging.DefaultInstance.SendAllAsync()
        return true;
    }
}

internal class ServiceExtender : IServiceExtender
{
    public IEnumerable<Type> GetHubTypes() { yield return typeof(NotificationHub); }
}
