using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.ASR;

namespace TrustInnova.Application.ASR
{
    public class ASRTaskCache
    {
        public ConcurrentDictionary<string, IASRTask> Caches { get; set; } = new ConcurrentDictionary<string, IASRTask>();
    }
}
