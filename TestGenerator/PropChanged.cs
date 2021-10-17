using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TestGenerator
{
    public class TestAttributeWithParameterAttribute : Attribute
    {
        public TestAttributeWithParameterAttribute(string name)
        {
            PropName = name;
        }

        public string PropName { get;  }
    }


    [AppBroker.ClassPropertyChangedAppbroker(true, false)]
    public partial class PropChanged2 : PropChanged
    {
        private int myPropertySecond;

    }

    [AppBroker.ClassPropertyChangedAppbroker(true, false)]
    public partial class PropChanged
    {
        private int myProperty;

        [AppBroker.IgnoreChangedField]
        private int myProperty2;
        [AppBroker.IgnoreField]
        private int myPropertyIgnored;


        [property: JsonPropertyName("transition_Time")]
        [AppBroker.PropertyChangedAppbroker(PropertyName = "Different2")]
        private int myProperty3;

        [property: System.Text.Json.Serialization.JsonPropertyName("transition_Time")]
        private int myProperty4;


        protected virtual void OnPropertyChanging<T>(ref T field, T value, string propertyName)
        {

        }
        protected virtual void OnPropertyChanged(string propertyName)
        {

        }
    }
}
