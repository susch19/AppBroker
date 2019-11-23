using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using nj = Newtonsoft.Json;
using AppBokerASP.Database.Model;
using AppBokerASP.Devices;
using System.Text;

namespace AppBokerASP
{
    public static class Extensions
    {
        private static JsonSerializerOptions opt;
        static Extensions()
        {
            opt = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(new System.Text.Encodings.Web.TextEncoderSettings(new System.Text.Unicode.UnicodeRange(0, 255)) )
            };
        }
        public static string ToJson<T>(this T t) => JsonSerializer.Serialize(t, opt);

        public static string ToJsonNj<T>(this T t) => nj.JsonConvert.SerializeObject(t);

        public static T ToObject<T>(this JsonElement element) => JsonSerializer.Deserialize<T>(element.GetRawText());
        public static T ToObject<T>(this string element) => JsonSerializer.Deserialize<T>(element);

        public static List<JsonElement> ToJsonElementList<T>(this T obj) => JsonSerializer.Deserialize<List<JsonElement>>(obj.ToJson());
        public static JsonElement ToJsonElement<T>(this T obj) => JsonSerializer.Deserialize<JsonElement>(obj.ToJsonNj());

        public static string[] ToStringArray(this ICollection<JsonElement> elements) => elements.Select(x => x.GetString()).ToArray();
        public static string[] ToRawStringArray(this ICollection<JsonElement> elements) => elements.Select(x => x.GetRawText()).ToArray();

        public static DeviceModel GetModel<T>(this T t) where T : Device => new DeviceModel { Id = t.Id, TypeName = t.TypeName };

        public static T GetDevice<T>(this DeviceModel model) where T : Device, new() => new T { Id = model.Id, TypeName = model.TypeName };



    }
}
