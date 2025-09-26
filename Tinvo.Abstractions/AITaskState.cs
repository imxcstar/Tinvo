using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Abstractions
{
    public class AITaskState
    {
        public string? TaskID { get; set; }

        public ChatHistory? ChatHistory { get; set; }

        public IAIChatTask? ChatTask { get; set; }

        public ChatSettings? ChatSettings { get; set; }

        public CancellationToken CancellationToken { get; set; } = default;
    }
}
