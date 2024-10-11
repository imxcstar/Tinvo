using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;
using WebApiClientCore;

namespace TrustInnova.Provider.OpenAI.API
{
    internal class NotNullJsonContentAttribute : HttpContentAttribute
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        protected override Task SetHttpContentAsync(ApiParameterContext context)
        {
            Stream stream;
            if (context.ParameterValue != null)
            {
                stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(context.ParameterValue, JsonSerializerOptions));
            }
            else
            {
                stream = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
            }

            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            context.HttpContext.RequestMessage.Content = content;
            return Task.CompletedTask;
        }
    }
}
