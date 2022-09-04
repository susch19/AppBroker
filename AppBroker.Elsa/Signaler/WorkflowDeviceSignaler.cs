
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
public class WorkflowDeviceSignaler : IWorkflowDeviceSignaler
{
    private static IServiceProvider? provider;

    public WorkflowDeviceSignaler(IServiceProvider serviceProvider)
    {
        provider = serviceProvider;
    }

    public static void DeviceChanged<TDevice, TValue>(TValue newValue, TValue oldValue, TDevice device, string deviceName, long deviceId, string typeName, [CallerMemberName] string propertyName = "")
    {

        if (provider is null || Equals(newValue, oldValue))
            return;

        using var scope = provider.CreateScope();
        var launchpad =scope.ServiceProvider.GetRequiredService<IWorkflowLaunchpad>();

        var model = new DeviceChangedEvent<TDevice, TValue>() { PropertyName = propertyName, NewValue = newValue, OldValue = oldValue, Device = device, DeviceName = deviceName, DeviceId = deviceId, TypeName = typeName };
        var bookmark = new DeviceChangedEventBookmark(propertyName, deviceName, deviceId, typeName);
        var launchContext = new WorkflowsQuery(nameof(DeviceChangedTrigger), bookmark);
        _ = launchpad.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(model));
    }
}