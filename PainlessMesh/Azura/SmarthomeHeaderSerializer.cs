using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PainlessMesh
{
    public static class SmarthomeHeaderSerialization
    {
        public static SmarthomeHeader Deserialize(Stream stream)
        {
            var smarthomeHeader = new SmarthomeHeader();

            smarthomeHeader.Version = byteSerialization.Deserialize(stream);
            smarthomeHeader.Type = (SmarthomePackageType)byteSerialization.Deserialize(stream);

            return smarthomeHeader;
        }
        public static void Deserialize(Stream stream, out SmarthomeHeader self) => self = Deserialize(stream);
        public static void Serialize(SmarthomeHeader self, Stream stream) => Serialize(in self, stream);
        public static void Serialize(this in SmarthomeHeader self, Stream stream)
        {
            byteSerialization.Serialize(self.Version, stream);
            byteSerialization.Serialize((byte)self.Type, stream);
        }

     
    }
}
