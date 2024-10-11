using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Application.AIAssistant.Entities;
using TrustInnova.Application.DataStorage;
using TrustInnova.Application.DB;

namespace TrustInnova.Application.AIAssistant
{
    public class AIAssistantService
    {
        private readonly DBData<AssistantEntity> _assistantDb;

        public AIAssistantService(DBData<AssistantEntity> assistantDb)
        {
            _assistantDb = assistantDb;
        }

        public async Task InitAsync()
        {
            await _assistantDb.InitAsync();
        }

        public List<AssistantEntity> GetAssistants()
        {
            return _assistantDb.Values.OrderByDescending(x => x.Index).ToList();
        }

        public async Task AddAssistantAsync(AssistantEntity assistant)
        {
            _assistantDb.TryAdd(assistant.Id, assistant);
            await _assistantDb.SaveChangeAsync();
        }

        public async Task RemoveAssistantByIdAsync(string id)
        {
            _assistantDb.Remove(id, out _);
            await _assistantDb.SaveChangeAsync();
        }

        public async Task UpdateAssistantAsync(AssistantEntity assistant)
        {
            _assistantDb.TryUpdate(assistant.Id, assistant, _assistantDb[assistant.Id]);
            await _assistantDb.SaveChangeAsync();
        }
    }
}
