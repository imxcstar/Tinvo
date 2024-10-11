using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ProviderTaskAttribute : Attribute
    {
        public string ID { get; set; }
        public string Name { get; set; }

        public ProviderTaskAttribute(string id, string name)
        {
            ID = id;
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class TypeMetadataDisplayNameAttribute : Attribute
    {
        public string Name { get; set; }

        public TypeMetadataDisplayNameAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class TypeMetadataAllowNullAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public class TypeMetadataDisplayStylesAttribute : Attribute
    {
        public int LineNumber { get; set; } = 1;
    }
}
