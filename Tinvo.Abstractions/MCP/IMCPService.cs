using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.MCP
{
    public interface IMCPService : IProvider
    {
        public Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default);
    }

    public abstract class MCPStreamService : IMCPService
    {
        public abstract Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default);
        public abstract Task InitAsync();
    }
}