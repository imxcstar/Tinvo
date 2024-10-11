using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Utils.Extend
{
    public static class AssemblyExtend
    {
        public static List<Assembly> GetReferanceAssemblies(this AppDomain domain)
        {
            var list = new List<Assembly>();
            var assemblies = domain.GetAssemblies();
            foreach (var item in assemblies)
            {
                GetReferanceAssemblies(item, list);
            }
            return list;
        }

        public static void GetReferanceAssemblies(Assembly assembly, List<Assembly> list)
        {
            var assemblies = assembly.GetReferencedAssemblies();
            foreach (var item in assemblies)
            {
                var ass = Assembly.Load(item);
                if (!list.Contains(ass))
                {
                    list.Add(ass);
                    GetReferanceAssemblies(ass, list);
                }
            }
        }
    }
}
