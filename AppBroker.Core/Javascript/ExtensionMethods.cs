using NiL.JS.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Javascript;
public static class ExtensionMethods
{
    public static Context DefineFunction(this Context context, string name, Delegate @delegate)
    {
        context.DefineVariable(name).Assign(JSValue.Marshal(@delegate));
        return context;
    }
}
