using AppBroker.Core;
using AppBroker.Core.Configuration;
using AppBroker.Core.Database;
using AppBroker.Notifications.Configuration;
using AppBroker.Notifications.Hubs;

using FirebaseAdmin.Messaging;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace AppBroker.Notifications.Controller;

public record AllOneTimeNotifications(List<string> Topics);

[Route("notification")]
public class NotificationController : ControllerBase
{
    private readonly IConfigManager configManager;

    public NotificationController(IConfigManager configManager)
    {
        this.configManager = configManager;
    }

    [HttpPost("sendNotification")]
    public async Task<ActionResult> SendNotification([FromBody] AppNotification notification)
    {
        await AppNotificationService.SendNotificationToDevices(notification);
        return Ok();
    }

    [HttpGet("firebaseOptions")]
    public Dictionary<string, FirebaseOptions>? GetFirebaseOptions()
    {
        return configManager.PluginConfigs.OfType<FirebaseConfig>().FirstOrDefault()?.Options;
    }

    [HttpGet("nextNotificationId")]
    public string NextNotificationId(string uniqueName, long? deviceId)
    {
        var deviceIdStr = deviceId?.ToString() ?? "";
        var key = $"{uniqueName}{deviceIdStr} Unique Notification";
        using var ctx = DbProvider.BrokerDbContext;
        var val = ctx.ConfigDatas.FirstOrDefault(x => x.Key == key);

        if (val is null)
        {
            val = ctx.ConfigDatas.Add(new() { Key = key, Value = $"{uniqueName}_{deviceIdStr}_{Guid.NewGuid()}" }).Entity;
            ctx.SaveChanges();
        }

        return val.Value;
    }

    [HttpGet("allOneTimeNotifications")]
    public async Task<AllOneTimeNotifications> AllOneTimeNotifications()
    {
        using var ctx = DbProvider.BrokerDbContext;
        return new AllOneTimeNotifications(await ctx.ConfigDatas
            .Where(x => EF.Functions.Like(x.Key, "% Unique Notification"))
            .Select(x => x.Value)
            .ToListAsync());
    }
}
