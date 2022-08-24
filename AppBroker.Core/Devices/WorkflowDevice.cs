using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace AppBroker.Core.Devices;

[RequiresPreviewFeatures]
public abstract partial class WorkflowDevice<TPropertySignaler, TDeviceSignaler> : Device
    where TPropertySignaler : IWorkflowPropertySignaler
    where TDeviceSignaler : IWorkflowDeviceSignaler
{
    public WorkflowDevice(long nodeId) : base(nodeId)
    {
        Logger = NLog.LogManager.GetCurrentClassLogger();
        Logger = Logger.WithProperty(nameof(Id), Id);
    }

    protected virtual void OnPropertyChanging<T>(ref T field, T value, [CallerMemberName] string? propertyName = "")
    {
        if (Initialized)
        {
            TPropertySignaler.PropertyChanged(value, field, FriendlyName, Id, TypeName, propertyName!);
            TDeviceSignaler.DeviceChanged(value, field, this, FriendlyName, Id, TypeName, propertyName!);
        }
        field = value;
    }
}
