using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AppBroker.Core.HelperMethods
{
    public class InstanceCreator<T>
    {
        public static Func<T> GetInstance;

        static InstanceCreator()
        {
            var body = Expression.New(typeof(T));
            GetInstance = Expression.Lambda<Func<T>>(body).Compile();
        }
    }
}
