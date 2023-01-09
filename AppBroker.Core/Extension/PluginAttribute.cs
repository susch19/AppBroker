using System;
using System.Collections.Generic;
using System.Text;

namespace AppBroker.Core.Extension
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PluginAttribute : Attribute
    {
        /// <summary>
        /// Determines the loading order of plugins, ordered by low to high
        /// </summary>
        public int LoadingPriority { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority">The priority of this plugin, lower means, it gets loaded before the ones with a higher number</param>
        public PluginAttribute(int priority)
        {
            LoadingPriority = priority;
        }

        public PluginAttribute()
        {
            LoadingPriority = 0;
        }
    }
}
