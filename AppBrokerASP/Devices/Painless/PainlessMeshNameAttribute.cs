using System;

namespace AppBrokerASP.Devices.Painless
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class PainlessMeshNameAttribute : Attribute
    {
        public PainlessMeshNameAttribute(string alternateName)
        {
            AlternateName = alternateName;
        }

        public string AlternateName { get; }
    }
}