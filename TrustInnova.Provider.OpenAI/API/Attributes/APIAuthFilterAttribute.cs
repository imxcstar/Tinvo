using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiClientCore.Attributes;
using WebApiClientCore;
using Microsoft.Extensions.DependencyInjection;

namespace TrustInnova.Provider.OpenAI.API
{
    internal class APIAuthFilterAttribute : ApiFilterAttribute
    {
        public override Task OnRequestAsync(ApiRequestContext context)
        {
            var authService = context.HttpContext.ServiceProvider.GetService<APIAuthService>();
            if (authService != null)
                context.HttpContext.RequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authService.Token);
            return Task.CompletedTask;
        }

        public override Task OnResponseAsync(ApiResponseContext context)
        {
            return Task.CompletedTask;
        }
    }

    public class APIAuthService
    {
        public string Token { get; set; }
    }
}
