using System;

namespace AppBokerASP.Devices
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