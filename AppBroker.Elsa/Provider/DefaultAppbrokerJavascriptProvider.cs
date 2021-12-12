using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AppBroker.Elsa.Models;

using Elsa.Scripting.JavaScript.Services;

using MediatR;

namespace AppBrokerASP.Devices.Elsa;

public class DefaultAppbrokerJavascriptProvider : TypeDefinitionProvider
{
    public override ValueTask<IEnumerable<Type>> CollectTypesAsync(TypeDefinitionContext context, CancellationToken cancellationToken = default)
    {
        //var types = InstanceContainer.DeviceManager.Devices.Values.Select(x => x.GetType()).ToArray();
        //return new ValueTask<IEnumerable<Type>>(types);
        return new ValueTask<IEnumerable<Type>>(new Type[] { typeof(DeviceChangedEvent), typeof(PropertyChangedEvent) } );
    }
}