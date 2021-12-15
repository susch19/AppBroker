using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Devices;

[RequiresPreviewFeatures]
public interface IWorkflowPropertySignaler
{
    public static abstract void PropertyChanged<T>(T newValue, T oldValue, string friendlyName, long id, string typeName, [CallerMemberName] string propertyName = "");
}
