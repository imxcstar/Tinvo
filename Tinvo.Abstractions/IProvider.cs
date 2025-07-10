using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Abstractions
{
    public interface IProvider
    {
        public Task InitAsync();
    }
}
