using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Devices;

[RequiresPreviewFeatures]
public interface IWorkflowDeviceSignaler
{
    public abstract static void DeviceChanged<TDevice, TValue>(TValue newValue, TValue oldValue, TDevice device, string deviceName, long deviceId, string typeName, [CallerMemberName] string propertyName = "");
}
