﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace AppBokerASP.Devices
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class DeviceNameAttribute : Attribute
    {
        public DeviceNameAttribute(string alternateName, params string[] alternativeNames)
        {
            PreferredName = alternateName;
            AlternativeNames = alternativeNames.ToList();
        }

        public string PreferredName { get; }

        public List<string> AlternativeNames{ get; set; }
    }
}