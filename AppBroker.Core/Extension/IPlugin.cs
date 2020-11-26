using System;

namespace AppBroker.Core
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

    }
}
