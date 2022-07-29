
using AppBroker.Activities;
using AppBroker.Core.Devices;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Models;

using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

using System.Runtime.CompilerServices;

namespace AppBroker.Elsa.Signaler;

public class WorkflowPropertySignaler : IWorkflowPropertySignaler
{
    private static IWorkflowLaunchpad scopedWorkflowLaunchpad;

    public WorkflowPropertySignaler(IWorkflowLaunchpad workflowLaunchpad)
    {
        scopedWorkflowLaunchpad = workflowLaunchpad;
    }

    public static void PropertyChanged<T>(T newValue, T oldValue, string friendlyName, long id, string typeName, [CallerMemberName] string propertyName = "")
    {
        if (scopedWorkflowLaunchpad is null || Equals(newValue, oldValue))
            return;

        var model = new PropertyChangedEvent<T>() { PropertyName = propertyName, NewValue = newValue, OldValue = oldValue, DeviceName = friendlyName, DeviceId = id, TypeName = typeName };
        var bookmark = new PropertyChangedEventBookmark(propertyName);
        var launchContext = new WorkflowsQuery(nameof(PropertyChangedTrigger), bookmark);
        _ = scopedWorkflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
    }
}