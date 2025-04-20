using AppBroker.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.PainlessMesh.Hubs;
public class PainlessHub
{
    public static void UpdateTime()
    {
        IInstanceContainer.Instance.GetDynamic<SmarthomeMeshManager>()?.UpdateTime();
    }
}
