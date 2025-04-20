using AppBroker.Core;
using AppBroker.Notifications.Hubs;

using AppBrokerASP;

using FirebaseAdmin.Messaging;

using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Timer = System.Timers.Timer;

namespace AppBroker.Notifications;

/// <summary>
/// 
/// </summary>
/// <param name="TopicFormat">Will be used for string.Format. Should not be empy. Parameters are DeviceId and TypeName</param>
/// <param name="DisplayName"></param>
/// <param name="DeviceId"></param>
/// <param name="TypeName"></param>
public record struct NotificationSetting(

    string TopicFormat,
    string DisplayName,
    long? DeviceId = null,
    string? TypeName = null);

public interface INotificationChecker
{
    /// <summary>
    /// Completes the message to make it sendable
    /// </summary>
    /// <param name="message">Non complete pre filled message</param>
    /// <returns>Null if no message should be send, otherwise the complete message</returns>
    Message? CreateMessage(Message message);

    IEnumerable<NotificationSetting> GetNotificationSettings();
}


public interface IStateChangeNotificationChecker : INotificationChecker
{
    bool ShouldNotify(StateChangeArgs e);
}

public interface ITimedNotificationChecker : INotificationChecker
{
    DateTimeOffset GetNextNotificationTime();
}

public class NotificationService
{
    private readonly Dictionary<Type, List<INotificationChecker>> notificationCheckers = new();
    private readonly List<ITimedNotificationChecker> notifyOnNextRun = new();
    private readonly Timer timer;

    internal NotificationService()
    {
        InstanceContainer.Instance.DeviceStateManager.StateChanged += StateManager_StateChanged;
        timer = new Timer(TimeSpan.FromSeconds(5));
        timer.Elapsed += Timer_Elapsed;
    }

    private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        foreach (var item in notifyOnNextRun)
        {
            foreach (var setting in item.GetNotificationSettings())
            {
                var topic = string.Format(setting.TopicFormat, setting.DeviceId, setting.TypeName);

                var message = new Message()
                {
                    Topic = topic,
                    Android = new AndroidConfig { Priority = Priority.High, TimeToLive = TimeSpan.FromSeconds(10) }
                };
                //AppNotificationService.SendNotificationToDevices(new VisibleAppNotification());
                if (message is not null)
                    message = item.CreateMessage(message);
                FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
        }
        notifyOnNextRun.Clear();

        foreach (ITimedNotificationChecker item in notificationCheckers[typeof(ITimedNotificationChecker)])
        {
            var next = item.GetNextNotificationTime();
            if (next > DateTimeOffset.UtcNow.AddSeconds(5))
                continue;
            notifyOnNextRun.Add(item);
        }
    }

    private void StateManager_StateChanged(object? sender, StateChangeArgs e)
    {
        if (notificationCheckers.TryGetValue(typeof(IStateChangeNotificationChecker), out var items))
        {
            foreach (IStateChangeNotificationChecker item in items)
            {
                if (item.ShouldNotify(e))
                {
                    var message = new Message()
                    {
                        Topic = InstanceContainer.Instance.ServerConfigManager.CloudConfig.ConnectionID + e.Id + e.PropertyName,
                        Android = new AndroidConfig { Priority = Priority.High, TimeToLive = TimeSpan.FromSeconds(10) }
                    };
                    message = item.CreateMessage(message);
                    if (message is not null)
                        FirebaseMessaging.DefaultInstance.SendAsync(message);
                }
            }
        }
    }

    public void RegisterNotificationChecker(INotificationChecker checker)
    {
        foreach (var inter in checker.GetType().GetInterfaces())
        {
            if (inter.IsAssignableFrom(typeof(INotificationChecker)))
            {
                ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(notificationCheckers, inter, out var exists);
                if (!exists)
                    list = [];
                list!.Add(checker);
            }
        }

        if (checker is ITimedNotificationChecker && !timer.Enabled)
        {
            timer.Start();
        }
    }
}
