using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions
{
    public class ProviderInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public List<Type> AllTaskType { get; set; }
        public List<Type> AllTaskConfigType { get; set; }
    }

    public class ProviderRegisterer
    {
        private List<ProviderInfo> _providers;

        public List<ProviderInfo> Providers => _providers;

        internal ProviderRegisterer()
        {
            _providers = new List<ProviderInfo>();
        }

        public ProviderRegisterer AddProviderInfo(ProviderInfo providerInfo)
        {
            _providers.Add(providerInfo);
            return this;
        }
    }

    public static class ProviderRegistererExtensions
    {
        public static ProviderRegisterer AddProviderRegisterer(this IServiceCollection services)
        {
            var ret = new ProviderRegisterer();
            services.AddSingleton(ret);
            return ret;
        }
    }
}
