using AppBroker.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PainlessMesh;

public static class ByteLengthListSerialization
{
    public static ByteLengthList Deserialize(Stream stream)
    {
        var count = byteSerialization.Deserialize(stream);
        var bl = new ByteLengthList();

        bl.AddRange(SerializationBase.DeserializeArray(stream, count, (b) =>
           {
               var innerCount = shortSerialization.Deserialize(stream);
               return SerializationBase.DeserializeArray(stream, innerCount, byteSerialization.Deserialize);
           }));

        return bl;
    }
    public static void Deserialize(Stream stream, out ByteLengthList self) => self = Deserialize(stream);
    public static void Serialize(this ByteLengthList self, Stream stream) => Serialize(in self, stream);
    public static void Serialize(in ByteLengthList self, Stream stream)
    {
        byteSerialization.Serialize((byte)self.Count, stream);
        var span = CollectionsMarshal.AsSpan(self);

        foreach (var item in span)
            innerSer(item, stream);
    }

    private static void innerSer(in byte[] t, Stream stream)
    {
        shortSerialization.Serialize((short)t.Length, stream);
        foreach (var item in t)
            byteSerialization.Serialize(item, stream);
    }
}
