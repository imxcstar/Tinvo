using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Utils.Extend
{
    public static class ObjectExtensions
    {
        public static T? GetPropertyValue<T>(this object obj, string propertyName) where T : class
        {
            return obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?
                .GetValue(obj) as T;
        }

        public static T? GetPrivatePropertyValue<T>(this object obj, string propertyName) where T : class
        {
            return obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic)?
                .GetValue(obj) as T;
        }
    }
}
