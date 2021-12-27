using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;


using Newtonsoft.Json.Linq;


namespace AppBroker.Core.Devices;



[ClassPropertyChangedAppbroker]
[RequiresPreviewFeatures]
public abstract partial class WorkflowDevice<TPropertySignaler, TDeviceSignaler>  : Device
    where TPropertySignaler : IWorkflowPropertySignaler
    where TDeviceSignaler : IWorkflowDeviceSignaler
{
    [AddOverride]
    private long id;
    [AddOverride]
    private string typeName;
    [AddOverride]
    private bool showInApp;
    [AddOverride]
    private string friendlyName;
    [AddOverride]
    private bool isConnected;

    public WorkflowDevice(long nodeId) : base(nodeId)
    {
        Initialized = false;
        id = nodeId;
        typeName = GetType().Name;
        isConnected = true;
        Logger = NLog.LogManager.GetCurrentClassLogger();
        Logger = Logger.WithProperty(nameof(Id), Id);
        friendlyName = "";
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
