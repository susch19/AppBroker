
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Core.Extension
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///  Has to contain emtpy ctor, otherwise not loaded
    /// </remarks>
    public interface IPlugin
    {
        public string Name { get; }

        bool Initialize(NLog.LogFactory logFactory);
        void RegisterTypes() { }

    }
}
