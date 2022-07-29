
using System.Diagnostics;

namespace TcpProxy;

public static class Extensions
{
    //private static JsonSerializerOptions opt;
    //static Extensions()
    //{
    //    opt = new JsonSerializerOptions
    //    {
    //        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(new System.Text.Encodings.Web.TextEncoderSettings(new System.Text.Unicode.UnicodeRange(0, 255)) )
    //    };
    //}
    //public static string ToJson<T>(this T t) => JsonSerializer.Serialize(t, opt);

    // No argument checking is done here. It is up to the caller.
    public static int ReadAtLeastCore(this Stream stream, Span<byte> buffer, int minimumBytes, bool throwOnEndOfStream)
    {
        Debug.Assert(minimumBytes <= buffer.Length);

        int totalRead = 0;
        while (totalRead < minimumBytes)
        {
            int read = stream.Read(buffer.Slice(totalRead));
            if (read == 0)
            {
                if (throwOnEndOfStream)
                {
                    throw new EndOfStreamException();
                }

                return totalRead;
            }

            totalRead += read;
        }

        return totalRead;
    }

    /// <summary>
    /// Reads bytes from the current stream and advances the position within the stream until the <paramref name="buffer"/> is filled.
    /// </summary>
    /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current stream.</param>
    /// <exception cref="EndOfStreamException">
    /// The end of the stream is reached before filling the <paramref name="buffer"/>.
    /// </exception>
    /// <remarks>
    /// When <paramref name="buffer"/> is empty, this read operation will be completed without waiting for available data in the stream.
    /// </remarks>
    public static void ReadExactly(this Stream stream, Span<byte> buffer) =>
        _ = stream.ReadAtLeastCore(buffer, buffer.Length, throwOnEndOfStream: true);


}
