﻿using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Abstractions
{
    public interface IFunctionManager
    {
        public List<FunctionInfo> GetFunctionInfos();
        public IAsyncEnumerable<IAIChatHandleMessage> CallFunctionAsync(string name, Dictionary<string, object?>? parameters, CancellationToken cancellationToken = default);
    }
}