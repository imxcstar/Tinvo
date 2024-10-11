using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Provider.Ollama
{
    public static class OllamaProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererOllamaProvider(this ProviderRegisterer registerer)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "Ollama",
                Name = "Ollama",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
}
