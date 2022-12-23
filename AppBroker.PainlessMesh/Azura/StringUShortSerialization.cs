using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace AppBroker.PainlessMesh;

public static class StringUShortSerialization
{
    //
    // Summary:
    //     Deserializes a string.
    //
    // Parameters:
    //   stream:
    //     Stream to read from.
    //
    // Returns:
    //     Value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Deserialize(Stream stream)
    {
        var num = ushortSerialization.Deserialize(stream);
        byte[] array = ArrayPool<byte>.Shared.Rent(num);
        try
        {
            stream.ReadArray(array, 0, num);
            return Encoding.UTF8.GetString(array, 0, num);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    //
    // Summary:
    //     Deserializes a string.
    //
    // Parameters:
    //   stream:
    //     Stream to read from.
    //
    //   self:
    //     Value.
    public static void Deserialize(Stream stream, out string self) => self = Deserialize(stream);

    //
    // Summary:
    //     Serializes a string.
    //
    // Parameters:
    //   self:
    //     Value.
    //
    //   stream:
    //     Stream to write to.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(this string self, Stream stream) => Serialize(in self, stream);

    //
    // Summary:
    //     Serializes a string.
    //
    // Parameters:
    //   self:
    //     Value.
    //
    //   stream:
    //     Stream to write to.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(in string self, Stream stream)
    {
        var byteCount = (ushort)Encoding.UTF8.GetByteCount(self);
        ushortSerialization.Serialize(byteCount, stream);
        byte[] array = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            _ = Encoding.UTF8.GetBytes(self, 0, self.Length, array, 0);
            stream.Write(array, 0, byteCount);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
