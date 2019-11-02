using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppBokerASP
{
    public static class Extensions
    {
        public static string ToJson<T>(this T t) => JsonSerializer.Serialize(t);

        public static T ToObject<T>(this JsonElement element) => JsonSerializer.Deserialize<T>(element.GetRawText());

    }
}
