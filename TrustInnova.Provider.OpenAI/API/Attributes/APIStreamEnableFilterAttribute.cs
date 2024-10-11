using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;
using WebApiClientCore;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace TrustInnova.Provider.OpenAI.API
{
    internal class APIStreamEnableFilterAttribute : ApiFilterAttribute
    {
        public override Task OnRequestAsync(ApiRequestContext context)
        {
            context.HttpContext.RequestMessage.SetBrowserResponseStreamingEnabled(true);
            return Task.CompletedTask;
        }

        public override Task OnResponseAsync(ApiResponseContext context)
        {
            return Task.CompletedTask;
        }
    }
}
