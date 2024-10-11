using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.KBS
{
    public enum KBSContentType
    {
        Text,
        Image,
        Video,
        Other
    }

    public interface IKBSTask
    {
        public IEnumerable<double> GetEmbeddings(Stream content, KBSContentType contentType);
    }
}
