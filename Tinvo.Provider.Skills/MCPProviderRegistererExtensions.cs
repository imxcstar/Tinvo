using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

namespace Tinvo.Provider.Skills
{
    public static class MCPProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererTinvoSkillsProvider(this ProviderRegisterer registerer)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "TinvoSkills",
                Name = "TinvoSkills",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
} 