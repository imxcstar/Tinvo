using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Provider.OpenAI.API
{
    internal class APIUtil
    {
        internal static T GetAPI<T>(string baseURL, string? token, string? proxy) where T : class
        {
            var apiServices = new ServiceCollection();
            var apiBuilder = apiServices
                .AddHttpApi<T>()
                .ConfigureHttpApi(o =>
                {
                    o.HttpHost = new Uri(baseURL);
                }).ConfigureHttpClient(c =>
                {
                    c.Timeout = TimeSpan.FromMinutes(10);
                    c.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token ?? "");
                });
            if (!string.IsNullOrWhiteSpace(proxy))
            {
                apiBuilder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseProxy = true,
                    Proxy = new WebProxy
                    {
                        Address = new Uri(proxy)
                    }
                });
            }
            var apiServiceProvider = apiServices.BuildServiceProvider();
            return apiServiceProvider.GetRequiredService<T>();
        }
    }
}
