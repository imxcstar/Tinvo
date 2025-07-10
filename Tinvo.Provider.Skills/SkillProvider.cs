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
        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;
        private readonly SkillConfig _config;

        private SkillManager _skillManager;
        private IDataStorageService _storageService;

        public override async Task InitAsync()
        {
            _storageService = await _dataStorageServiceFactory.CreateAsync();
            _skillManager = new SkillManager(_storageService, _config);
        }

        public SkillProvider(IDataStorageServiceFactory dataStorageServiceFactory, SkillConfig config)
        {
            _dataStorageServiceFactory = dataStorageServiceFactory;
            _config = config;
        }

        public override Task<IFunctionManager> GetIFunctionManager(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IFunctionManager>(_skillManager);
        }
    }
}
