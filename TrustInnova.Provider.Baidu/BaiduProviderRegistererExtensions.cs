using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Provider.Baidu
{
    public static class BaiduProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererBaiduProvider(this ProviderRegisterer registerer)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "Baidu",
                Name = "百度",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
}
