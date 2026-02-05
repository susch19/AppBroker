namespace AppBroker.Plugins.Extension
{

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///  Has to contain emtpy ctor, otherwise not loaded
    /// </remarks>
    public interface IPlugin
    {
        string Name { get; }
        /// <summary>
        /// Gets the numeric order for when to load the plugin, higher numbers means loaded later
        /// </summary>
        int LoadOrder { get; }

        bool Initialize(NLog.LogFactory logFactory);
        void RegisterTypes() { }

    }


    public interface IAppConfigurator
    {
        string UniqueName { get; }
        IDictionary<string, string>? GetConfigs();
    }

}
