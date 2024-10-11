using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.TTS;

namespace TrustInnova.Application.TTS
{
    public class TTSTaskCache
    {
        public ConcurrentDictionary<string, ITTSTask> Caches { get; set; } = new ConcurrentDictionary<string, ITTSTask>();
    }
}
