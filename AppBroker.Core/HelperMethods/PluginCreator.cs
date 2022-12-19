using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AppBroker.Core.HelperMethods
{
    public class PluginCreator<T>
    {
        public static T GetInstance(Type pluginType)
        {
            var body = Expression.New(pluginType);
            return Expression.Lambda<Func<T>>(body).Compile().Invoke();
        }
    }
}
