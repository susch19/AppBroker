using AppBrokerASP.Database.Model;
using AppBrokerASP.Devices;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AppBrokerASP
{
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

        public static string ToJson<T>(this T t) => JsonConvert.SerializeObject(t);

        public static T ToDeObject<T>(this JToken element) => JsonConvert.DeserializeObject<T>(element.ToString())!;
        public static T ToDeObject<T>(this string element) => JsonConvert.DeserializeObject<T>(element)!;

        public static List<JToken> ToJTokenList<T>(this IEnumerable<T> obj) => obj.Select(x => JToken.FromObject(x!)).ToList();  //JsonConvert.DeserializeObject<List<JToken>>(obj.ToJson());
        public static JToken ToJToken<T>(this T obj) => JToken.FromObject(obj!);// JsonConvert.DeserializeObject<JToken>(obj.ToJson());

        public static string[] ToStringArray(this ICollection<JToken> elements) => elements.Select(x => x.ToString()).ToArray();
        public static string[] ToRawStringArray(this ICollection<JToken> elements) => elements.Select(x => x.ToString()).ToArray();

        public static DeviceModel GetModel<T>(this T t) where T : Device => new() { Id = t.Id, TypeName = t.TypeName, FriendlyName = t.FriendlyName };

        public static T GetDevice<T>(this DeviceModel model) where T : Device, new() => new() { Id = model.Id, TypeName = model.TypeName, FriendlyName = model.FriendlyName };



    }
}
