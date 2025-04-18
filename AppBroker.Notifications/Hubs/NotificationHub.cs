using AppBroker.Core;
using AppBroker.Core.Database;
using AppBroker.Notifications.Configuration;

using FirebaseAdmin.Messaging;

using Microsoft.AspNetCore.SignalR;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AppBroker.Notifications.Hubs;



public class VisibleAppNotification : AppNotification
{
    public string Title { get; set; }
    public string? Body { get; set; }
    public int? TTL { get; set; }
    public VisibleAppNotification(string title, string topic) : base(topic)
    {
        Title = title;
    }

    public override Message? ConvertToFirebaseMessage()
    {
        return new Message()
        {
            Notification = new Notification { Title = Title, Body = Body ?? "" },
            Topic = Topic,
            Android = new AndroidConfig { Priority = Priority.High, TimeToLive = TTL == null ? null : TimeSpan.FromSeconds(TTL.Value) }
        };
    }
}

public abstract class AppNotification
{
    public string TypeName => this.GetType().Name;
    public string Topic { get; set; }
    public bool WasOneTime { get; set; }
    public AppNotification(string topic)
    {
        Topic = topic;
    }

    public virtual Message? ConvertToFirebaseMessage() => null;
}

public static partial class AppNotificationService
{
    internal static ConcurrentDictionary<string, (CancellationToken, dynamic)> NotificationEnabledClients { get; } = new();
    internal static DynamicHub NotificationHub { get; set; }

    [GeneratedRegex("[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")]
    private static partial Regex EndsWithGuid();


    public static async Task SendNotificationToDevices(AppNotification notification)
    {
        var couldBeOneTime = EndsWithGuid().IsMatch(notification.Topic);
        if (couldBeOneTime)
        {
            using var ctx = DbProvider.BrokerDbContext;
            var val = ctx.ConfigDatas.FirstOrDefault(x => x.Value == notification.Topic);

            if (val is not null)
            {
                ctx.ConfigDatas.Remove(val);
                ctx.SaveChanges();
                notification.WasOneTime = true;
            }
        }

        var firebaseMsg = notification.ConvertToFirebaseMessage();
        if (firebaseMsg is not null)
            await FirebaseMessaging.DefaultInstance.SendAsync(firebaseMsg);


        foreach (var item in NotificationEnabledClients.ToArray())
        {
            if (item.Value.Item1.IsCancellationRequested)
            {
                NotificationEnabledClients.TryRemove(item);
                continue;
            }
            item.Value.Item2.Notify(notification);
        }
    }
}

public class NotificationHub
{
    public static Dictionary<string, FirebaseOptions>? GetFirebaseOptions()
    {
        return IInstanceContainer.Instance.ConfigManager.PluginConfigs.OfType<FirebaseConfig>().FirstOrDefault()?.Options;
    }

    public static void Activate(DynamicHub hub)
    {
        if (AppNotificationService.NotificationHub != hub)
        {
            AppNotificationService.NotificationHub = hub;
        }
        if (!AppNotificationService.NotificationEnabledClients.ContainsKey(hub.Context.ConnectionId))
        {
            AppNotificationService.NotificationEnabledClients[hub.Context.ConnectionId] = (hub.Context.ConnectionAborted, hub.Clients.Caller);
        }

        var toDelete = AppNotificationService
            .NotificationEnabledClients
            .Where(x => x.Value.Item1.IsCancellationRequested)
            .ToList();
        foreach (var item in toDelete)
        {
            AppNotificationService.NotificationEnabledClients.TryRemove(item);
        }
    }

}


