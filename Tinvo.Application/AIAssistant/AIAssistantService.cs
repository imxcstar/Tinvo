using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Abstractions.DB;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;

namespace Tinvo.Application.AIAssistant
{
    public class AIAssistantService
    {
        private readonly LinkedDB<AssistantEntity> _assistantDb;

        public AIAssistantService(LinkedDB<AssistantEntity> assistantDb)
        {
            _assistantDb = assistantDb;
        }

        public async Task InitAsync()
        {
            await _assistantDb.InitAsync();
        }

        public async Task<List<AssistantEntity>> GetAssistantsAsync()
        {
            var ret = new List<AssistantEntity>();
            await foreach (var item in _assistantDb.GetAllAsync())
            {
                ret.Add(item);
            }
            return ret.OrderByDescending(x => x.Index).ToList();
        }

        public async Task AddAssistantAsync(AssistantEntity assistant)
        {
            await _assistantDb.AddAsync(assistant);
        }

        public async Task RemoveAssistantByIdAsync(string id)
        {
            await _assistantDb.DeleteAsync(id);
        }

        public async Task RemoveAllAssistantAsync()
        {
            await _assistantDb.ClearAllDataAsync();
        }

        public async Task UpdateAssistantAsync(AssistantEntity assistant)
        {
            await _assistantDb.UpdateAsync(assistant);
        }

        public async Task<string> ExportJsonTextAsync()
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
            return await _assistantDb.ExportJsonTextAsync(serializerOptions);
        }

        public async Task ImportAsync(Stream values)
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
            await _assistantDb.ImportAsync(values, serializerOptions);
        }
    }
}
