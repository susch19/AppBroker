using Azura;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PainlessMesh
{
    public class ByteLengthList : List<byte[]>
    {
        public ByteLengthList() : base()
        {

        }
        public ByteLengthList(IEnumerable<byte[]> byteArrays) : base(byteArrays)
        {
        }
        public ByteLengthList(byte capacity) : base(capacity)
        {

        }
    }

    //public class ABC
    //{
    //    public ABC()
    //    {
    //        var testSer = new TestSer(new AzuraContext());
    //        testSer.Serialize(null);
    //    }
    //}
}
