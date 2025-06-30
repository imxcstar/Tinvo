using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.MCP;
using Tinvo.Application.DataStorage;

namespace Tinvo.Provider.Skill.Image
{
    [ProviderTask("Tinvo.MCP.Skill.Image", "图片处理")]
    public class SkillProvider : MCPStreamService
    {
        private SkillManager _skillManager;

        public SkillProvider(IDataStorageService dataStorageService)
        {
            _skillManager = new SkillManager(dataStorageService);
        }

        public override Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFunctionManager>(_skillManager);
        }
    }
}
