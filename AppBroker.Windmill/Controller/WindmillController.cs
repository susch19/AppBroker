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
using AppBroker.Windmill.Model;

namespace AppBroker.Windmill.Controller;


/// <summary>
/// Specific Controller for Windmill Workflow Engine
/// </summary>
[Route("windmill")]
public class WindmillController : ControllerBase
{
    private readonly IDeviceManager deviceManager;

    public WindmillController(IDeviceManager deviceManager)
    {
        this.deviceManager = deviceManager;
    }

    /// <summary>
    /// Used to update things on the app
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task Update([FromBody] WindmillSmarthomeMessage message)
    {
        message.NodeId = long.Parse(message.NodeIdHex, System.Globalization.NumberStyles.HexNumber);

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


}
