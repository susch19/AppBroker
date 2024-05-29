using AppBroker.Core;

using FirebaseAdmin.Messaging;

using Microsoft.AspNetCore.Mvc;


namespace AppBroker.Notifications.Controller;
[Route("device")]
public class NotificationController : ControllerBase
{

    public NotificationController()
    {
    }

    [HttpGet("SendNotification")]
    public async Task<ActionResult> UpdateTime()
    {
        var exactTime = DateTime.UtcNow.ToString();
        var res = await FirebaseMessaging.DefaultInstance.SendAsync(new Message()
        {
            Notification = new Notification { Title = "Test", Body = $"Test Body {exactTime}" },
            Data = new Dictionary<string, string>() { { "A", "b" }, { "c", exactTime } },
            Topic = "testsmarthome",
            Android = new AndroidConfig { Priority = Priority.High, TimeToLive = TimeSpan.FromSeconds(5) }
        });
        return Ok(res);
    }
}
