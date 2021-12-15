using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Scripting.JavaScript.Services;

using MediatR;

namespace AppBrokerASP.Devices.Elsa;

public class DeviceJavascriptProvider : TypeDefinitionProvider
{
    public override ValueTask<IEnumerable<Type>> CollectTypesAsync(TypeDefinitionContext context, CancellationToken cancellationToken = default)
    {
        var types = IInstanceContainer.Instance.DeviceManager.Devices.Values.Select(x => x.GetType()).ToArray();
        return new ValueTask<IEnumerable<Type>>(types);
    }
}