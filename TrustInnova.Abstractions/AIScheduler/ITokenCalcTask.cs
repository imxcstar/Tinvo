using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Abstractions.AIScheduler
{
    public interface ITokenCalcTask
    {
        public long GetTokens(string value);
        public long GetTokens(Stream value, ChatMessageContentType contentType);
    }
}
