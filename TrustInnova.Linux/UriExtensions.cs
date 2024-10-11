using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova
{
    public static class UriExtensions
    {
        public static bool IsBaseOfPage(this Uri baseUri, string? uriString)
        {
            if (Path.HasExtension(uriString))
                return false;

            var uri = new Uri(uriString!);
            return baseUri.IsBaseOf(uri);
        }
    }
}
