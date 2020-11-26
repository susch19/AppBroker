using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppBroker.Core.Controller
{
    [ExtractJwtSessionFilter]
    public class BaseController : ControllerBase
    {
        public static readonly JsonSerializerSettings DefaultJsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter> { new EmptyToNullConverter() },
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public AppBrokerSession Session;

        //protected JsonResult Json<T>(T obj) where T : class
        //{

        //    return new JsonResult(obj, DefaultJsonSettings);
        //}
    }
    public class AppBrokerSession
    {
        public int Id;
        public DateTime Expires;
    }

    public class ExtractJwtSessionFilterAttribute : Attribute, IAsyncResourceFilter
    {
        private const string ItemDicKey = "jwt_extract";
        public static readonly byte[] JwtKeyBytes;

        static ExtractJwtSessionFilterAttribute()
        {
            JwtKeyBytes = File.ReadAllBytes("jwt.key");
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (context.HttpContext.Request.Headers.TryGetValue("X-Token", out StringValues sv) && sv.Count > 0)
            {
                if (JsonWebToken.Decode(sv[0], JwtKeyBytes, true, out AppBrokerSession s))
                    context.HttpContext.Items[ItemDicKey] = s;
            }

            await next.Invoke();
        }

        public static AppBrokerSession FromItems(HttpContext context)
        {
            AppBrokerSession session = null;
            if (context.Items.TryGetValue(ItemDicKey, out object s))
            {
                session = (AppBrokerSession)s;
            }
            return session;
        }
    }
    public class EmptyToNullConverter : JsonConverter
    {
        private readonly JsonSerializer _stringSerializer = new JsonSerializer();

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = _stringSerializer.Deserialize<string>(reader);

            if (string.IsNullOrEmpty(value))
            {
                value = null;
            }

            return value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            _stringSerializer.Serialize(writer, value);
        }
    }
}
