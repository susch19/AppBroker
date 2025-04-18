using AppBroker.Core.Database.Model;
using AppBroker.Core.Devices;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Diagnostics;

namespace AppBroker.Core;

public static class Extensions
{
    public static readonly JsonSerializerSettings TypeSerializeSetting;

    static Extensions()
    {

        TypeSerializeSetting = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
            
        };
    }

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
            int read = stream.Read(buffer[totalRead..]);
            if (read == 0)
            {
                if (throwOnEndOfStream)
                    throw new EndOfStreamException();

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

    public static string ToJson<T>(this T t) => JsonConvert.SerializeObject(t);
    public static string ToJsonTyped<T>(this T t) => JsonConvert.SerializeObject(t, TypeSerializeSetting);

    public static T FromJsonTyped<T>(this string s) => JsonConvert.DeserializeObject<T>(s, TypeSerializeSetting)!;
    public static object? FromJsonTyped(this string s) => JsonConvert.DeserializeObject(s, TypeSerializeSetting)!;

    public static T ToDeObject<T>(this JToken element) => JsonConvert.DeserializeObject<T>(element.ToString())!;
    public static T ToDeObject<T>(this string element) => JsonConvert.DeserializeObject<T>(element)!;
    public static object? ToOObject(this JToken token)
    {
        return token.Type switch
        {
            JTokenType.Integer => token.Value<long>(),
            JTokenType.Float => token.Value<double>(),
            JTokenType.String or JTokenType.Guid or JTokenType.Uri => token.Value<string>(),
            JTokenType.Boolean => token.Value<bool>(),
            JTokenType.Date => token.Value<DateTime>(),
            JTokenType.TimeSpan => token.Value<TimeSpan>(),
            JTokenType.Object => token.Value<object>(),
            JTokenType.None => null,
            JTokenType.Null => null,
            JTokenType.Bytes => token.ToString(),
            JTokenType.Undefined => token.ToString(),
            JTokenType.Raw => token.ToString(),
            _ => token.ToString(),
        };
    }

    public static List<JToken> ToJTokenList<T>(this IEnumerable<T> obj) => obj.Select(x => JToken.FromObject(x!)).ToList();  //JsonConvert.DeserializeObject<List<JToken>>(obj.ToJson());
    public static JToken ToJToken<T>(this T obj) => JToken.FromObject(obj!);// JsonConvert.DeserializeObject<JToken>(obj.ToJson());

    public static string[] ToStringArray(this ICollection<JToken> elements) => elements.Select(x => x.ToString()).ToArray();
    public static string[] ToRawStringArray(this ICollection<JToken> elements) => elements.Select(x => x.ToString()).ToArray();

    public static DeviceModel GetModel<T>(this T t) where T : Device => new() { Id = t.Id, TypeName = t.TypeName, FriendlyName = t.FriendlyName, StartAutomatically = t.StartAutomatically };

    public static T GetDevice<T>(this DeviceModel model) where T : Device, new() => new() { Id = model.Id, TypeName = model.TypeName, FriendlyName = model.FriendlyName ?? "" };

}
