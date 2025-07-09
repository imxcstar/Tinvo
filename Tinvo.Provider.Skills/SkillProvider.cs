using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.Skills
{
    [ProviderTask("Tinvo.Skills", "技能集合")]
    public class SkillProvider : MCPStreamService
    {
        private SkillManager _skillManager;

        public SkillProvider(IDataStorageServiceFactory dataStorageServiceFactory, SkillConfig config)
        {
            _skillManager = new SkillManager(dataStorageServiceFactory.Create(), config);
        }

        public override Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFunctionManager>(_skillManager);
        }
    }
}
