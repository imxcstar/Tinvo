using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Utils.Extend
{
    public static class DictionaryExtend
    {
        public static T GetValueOrDefaultTypeValue<T>(this Dictionary<string, object>? dic, string key, T defaultValue)
        {
            if (dic == null)
                return defaultValue;
            var retObj = dic.GetValueOrDefault(key, defaultValue!);
            try
            {
                return (T)retObj;
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }
    }
}
