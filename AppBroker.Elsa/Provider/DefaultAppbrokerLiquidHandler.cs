using Elsa.Scripting.Liquid.Messages;
using MediatR;
using Fluid;
using System.Threading.Tasks;
using System.Threading;
using AppBroker.Elsa.Models;

namespace AppBrokerASP.Devices.Elsa;

public class DefaultAppbrokerLiquidHandler : INotificationHandler<EvaluatingLiquidExpression>
{
    public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
    {

        notification.TemplateContext.Options.MemberAccessStrategy.Register<PropertyChangedEvent>();
        notification.TemplateContext.Options.MemberAccessStrategy.Register<DeviceChangedEvent>();

        return Task.CompletedTask;
    }
}