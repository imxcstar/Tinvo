using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.AIScheduler
{
    public interface ITokenCalcTask
    {
        public long GetTokens(IAIChatHandleMessage value);
    }
}
