using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.DB
{
    public interface IDBEntity
    {
        public string Id { get; set; }
    }
}
