
using AppBroker.Activities;
using AppBroker.Core.Devices;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Models;

using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

using Microsoft.Extensions.DependencyInjection;

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;

namespace AppBroker.Elsa.Signaler;

[RequiresPreviewFeatures]
public class WorkflowPropertySignaler : IWorkflowPropertySignaler
{
    private static IServiceProvider? provider;

    public WorkflowPropertySignaler(IServiceProvider serviceProvider)
    {
        provider = serviceProvider;
    }

    public static void PropertyChanged<T>(T newValue, T oldValue, string friendlyName, long id, string typeName, [CallerMemberName] string propertyName = "")
    {
        if (provider is null || Equals(newValue, oldValue))
            return;
        using var scope = provider.CreateScope();
        var launchpad = scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();

        var model = new PropertyChangedEvent<T>() { PropertyName = propertyName, NewValue = newValue, OldValue = oldValue, DeviceName = friendlyName, DeviceId = id, TypeName = typeName };
        var bookmark = new PropertyChangedEventBookmark(propertyName);
        var launchContext = new WorkflowsQuery(nameof(PropertyChangedTrigger), bookmark);
        _ = launchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
    }
}