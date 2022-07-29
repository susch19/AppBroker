
using AppBroker.Activities;
using AppBroker.Core.Devices;
using AppBroker.Elsa.Bookmarks;
using AppBroker.Elsa.Models;

using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;

namespace AppBroker.Elsa.Signaler;

public class WorkflowDeviceSignaler : IWorkflowDeviceSignaler
{
    private static IWorkflowLaunchpad scopedWorkflowLaunchpad;

[RequiresPreviewFeatures]
public class WorkflowDeviceSignaler : IWorkflowDeviceSignaler
{
    private static IWorkflowLaunchpad scopedWorkflowLaunchpad;

    public WorkflowDeviceSignaler(IWorkflowLaunchpad workflowLaunchpad)
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
        _ = scopedWorkflowLaunchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
    }
}