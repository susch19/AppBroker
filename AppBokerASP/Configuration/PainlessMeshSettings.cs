using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AppBokerASP.Configuration
{
    public class PainlessMeshSettings
    {
        public const string ConfigName = nameof(PainlessMeshSettings);

        public bool Enabled { get; set; }
        public ushort ListenPort { get; set; }
    }
}
