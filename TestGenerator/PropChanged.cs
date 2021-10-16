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
    public partial class PropChanged
    {
        private int myProperty;
        
        [AppBroker.IgnoreChangedField]
        private int myProperty2;
        [AppBroker.IgnoreField]
        private int myPropertyIgnored;

        [AppBroker.PropertyChangedAppbroker(PropertyName = "Different"), AppBroker.CopyPropertyAttributesFromAttribute(nameof(property))]
        private int myProperty3;

        [JsonIgnore(), Description("For"), JsonPropertyName("Test")]
        private bool property { get; }

        protected virtual void OnPropertyChanging<T>(ref T field, T value, string propertyName)
        {

        }
        protected virtual void OnPropertyChanged(string propertyName)
        {

        }
    }
}
