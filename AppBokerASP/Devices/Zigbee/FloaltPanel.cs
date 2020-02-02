﻿using Newtonsoft.Json.Linq;
using PainlessMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AppBokerASP.Devices.Zigbee
{
    public class FloaltPanel : ZigbeeLamp
    {
        public FloaltPanel(long nodeId, string baseUpdateUrl) : base(nodeId, baseUpdateUrl, typeof(FloaltPanel))
        {
        }
    }
}
