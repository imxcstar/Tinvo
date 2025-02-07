using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Provider.Thor.API
{
    internal class APIUtil
    {
        internal static T GetAPI<T>(string baseURL, string? auth, string? proxy) where T : class
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
                    if (!string.IsNullOrWhiteSpace(auth))
                        c.DefaultRequestHeaders.Add("x-thor-chat-auth", auth);
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
