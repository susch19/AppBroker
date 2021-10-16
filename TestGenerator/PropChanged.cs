using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator
{
    [AppBroker.ClassPropertyChangedAppbroker(true, false)]
    public partial class PropChanged
    {
        private int myProperty;
        
        [AppBroker.IgnoreChangedAppbroker]
        private int myProperty2;

        [AppBroker.PropertyChangedAppbroker(PropertyName = "Different")]
        private int myProperty3;

        protected virtual void OnPropertyChanging<T>(ref T field, T value, string propertyName)
        {

        }
        protected virtual void OnPropertyChanged(string propertyName)
        {

        }
    }
}
