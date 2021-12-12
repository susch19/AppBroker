
using AppBroker.Elsa.Activities;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Models;

using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.Workflows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AppBroker.Elsa.Signaler;

public class WorkflowPropertySignaler
{
    private static Scoped<IWorkflowLaunchpad> scopedWorkflowLaunchpad;

    public WorkflowPropertySignaler(Scoped<IWorkflowLaunchpad> workflowLaunchpad)
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
        _ = scopedWorkflowLaunchpad.UseService(async s => await s.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model)));
    }
}
