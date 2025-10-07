using AppBroker.Core;

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppBroker.Runtime.Swagger;
internal class EnumSchemaFilter : ISchemaFilter
{
    private Type[] intEnums = [typeof(Command)];
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (intEnums.Contains(context.Type))
        {
            schema.Enum.Clear();
            schema.Type = "integer";

            var enumUnderlyingType = Enum.GetUnderlyingType(context.Type);
            schema.Format = enumUnderlyingType.Name.ToLower();
        }
        else if (context.Type.IsEnum)
        {
            var array = new OpenApiArray();
            array.AddRange(Enum.GetNames(context.Type).Select(n => new OpenApiString(n)));
            // NSwag
            schema.Extensions.Add("x-enumNames", array);
            // Openapi-generator
            schema.Extensions.Add("x-enum-varnames", array);
        }
    }
}
