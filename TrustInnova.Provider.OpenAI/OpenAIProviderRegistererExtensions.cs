using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Provider.OpenAI
{
    public static class OpenAIProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererOpenAIProvider(this ProviderRegisterer registerer)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "OpenAI",
                Name = "OpenAI",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
}
