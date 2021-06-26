using System;

namespace AppBokerASP.Devices
{
    internal class PainlessMeshNameAttribute : Attribute
    {
        public PainlessMeshNameAttribute(string alternateName)
        {
            AlternateName = alternateName;
        }

        public string AlternateName { get; }
    }
}