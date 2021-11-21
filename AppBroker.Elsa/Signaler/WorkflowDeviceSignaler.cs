
using AppBroker.Elsa.Activities;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Models;

using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Services.Workflows;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AppBroker.Elsa.Signaler;

public class WorkflowDeviceSignaler
{
    private static Scoped<IWorkflowLaunchpad> scopedWorkflowLaunchpad;

    public WorkflowDeviceSignaler(Scoped<IWorkflowLaunchpad> workflowLaunchpad)
    {
        scopedWorkflowLaunchpad = workflowLaunchpad;
    }

    public static void DeviceChanged<TDevice, TValue>(TValue newValue, TValue oldValue, TDevice device, string deviceName, long deviceId, string typeName, [CallerMemberName] string propertyName = "")
    {
        if (scopedWorkflowLaunchpad is null || Equals(newValue, oldValue))
            return;

        var model = new DeviceChangedEvent<TDevice, TValue>() { PropertyName = propertyName, NewValue = newValue, OldValue = oldValue, Device = device, DeviceName = deviceName, DeviceId = deviceId, TypeName = typeName };
        var bookmark = new DeviceChangedEventBookmark(propertyName, deviceName, deviceId, typeName);
        var launchContext = new WorkflowsQuery(nameof(DeviceChangedTrigger), bookmark);
        _ = scopedWorkflowLaunchpad.UseService(s => s.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model)));
    }
}
