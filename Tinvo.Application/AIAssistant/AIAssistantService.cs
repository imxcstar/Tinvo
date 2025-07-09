using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.AIAssistant.Entities;
using Tinvo.Application.DataStorage;
using Tinvo.Application.DB;

namespace Tinvo.Application.AIAssistant
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

        public async Task RemoveAllAssistantAsync()
        {
            _assistantDb.Clear();
            await _assistantDb.SaveChangeAsync();
        }

        public async Task UpdateAssistantAsync(AssistantEntity assistant)
        {
            _assistantDb.TryUpdate(assistant.Id, assistant, _assistantDb[assistant.Id]);
            await _assistantDb.SaveChangeAsync();
        }

        public async Task UpdateAssistantAsync()
        {
            await _assistantDb.SaveChangeAsync();
        }

        public string ExportJsonText()
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
            return _assistantDb.ExportJsonText(serializerOptions);
        }

        public async Task ImportAsync(Stream values)
        {
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
            await _assistantDb.ImportAsync(values, serializerOptions);
            await _assistantDb.SaveChangeAsync();
        }
    }
}
