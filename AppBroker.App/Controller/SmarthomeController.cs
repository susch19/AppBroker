using AppBroker.Core.Devices;
using AppBroker.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppBroker.Core.Managers;
using AppBrokerASP;
using System.Text.Json;
using NLog;

namespace AppBroker.App.Controller;

/// <summary>
/// Test summary for smarthome controller
/// </summary>
[Route("app/smarthome")]
public class SmarthomeController : ControllerBase
{
    private readonly IDeviceManager deviceManager;
    private readonly NLog.ILogger logger;

    public SmarthomeController(IDeviceManager deviceManager, ILogger logger)
    {
        this.deviceManager = deviceManager;
        this.logger = logger;
    }

    /// <summary>
    /// Used to update things on the app
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task Update([FromBody] JsonApiSmarthomeMessage message)
    {
        if (deviceManager.Devices.TryGetValue(message.NodeId, out Device? device))
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
        }
    }

    [HttpGet]
    public dynamic? GetConfig([FromQuery] long deviceId) => deviceManager.Devices.TryGetValue(deviceId, out Device? device) ? device.GetConfig() : null;

    [HttpPost("log")]
    public void Log([FromBody] List<AppLog> logLines)
    {
        foreach (var item in logLines)
        {
            var info = new LogEventInfo(
                item.LogLevel switch
                {
                    AppLogLevel.Fatal => LogLevel.Fatal,
                    AppLogLevel.Error => LogLevel.Error,
                    AppLogLevel.Warning => LogLevel.Warn,
                    AppLogLevel.Info => LogLevel.Info,
                    AppLogLevel.Debug => LogLevel.Debug,
                    _ => LogLevel.Debug,
                }
                , item.LoggerName, item.Message);
            info.TimeStamp = item.TimeStamp;
            logger.Log(info);
        }
    }

}

public enum AppLogLevel
{
    Fatal,
    Error,
    Warning,
    Info,
    Debug
}
public record struct AppLog(AppLogLevel LogLevel, DateTime TimeStamp, string Message, string? LoggerName);
