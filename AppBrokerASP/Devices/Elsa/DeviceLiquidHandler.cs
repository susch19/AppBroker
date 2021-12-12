﻿using Elsa.Scripting.Liquid.Messages;
using MediatR;
using Fluid;
using System.Threading.Tasks;
using System.Threading;

namespace AppBrokerASP.Devices.Elsa;

public class DeviceLiquidHandler : INotificationHandler<EvaluatingLiquidExpression>
{
    public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
    {
        foreach (var item in InstanceContainer.DeviceManager.Devices.Values)
        {
            notification.TemplateContext.Options.MemberAccessStrategy.Register(item.GetType());
        } 

        return Task.CompletedTask;
    }
}