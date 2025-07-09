using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tinvo.Application.DataStorage;

namespace Tinvo.Application.DB
{
    public class DBData<T> : ConcurrentDictionary<string, T> where T : IDBEntity
    {
        private IDataStorageService _dataStorageService;

        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;
        private readonly string _name;

        public DBData(IDataStorageServiceFactory dataStorageServiceFactory)
        {
            _dataStorageServiceFactory = dataStorageServiceFactory;
            _name = typeof(T).GetCustomAttributes<TableAttribute>(false).FirstOrDefault()?.Name ?? typeof(T).Name.Replace("Entity", "");
            _name = $"db_{_name}";
        }

        public async Task InitAsync()
        {
            _dataStorageService = await _dataStorageServiceFactory.CreateAsync();
            var data = await _dataStorageService.GetItemAsync<List<T>>(_name) ?? new List<T>();
            foreach (var item in data)
            {
                this.TryAdd(item.Id, item);
            }
        }

        public async Task SaveChangeAsync()
        {
            await _dataStorageService.SetItemAsync(_name, this.Values);
        }

        public string ExportJsonText(JsonSerializerOptions jsonSerializerOptions)
        {
            return JsonSerializer.Serialize(this.Values, jsonSerializerOptions);
        }

        public async Task ImportAsync(Stream values, JsonSerializerOptions jsonSerializerOptions)
        {
            this.Clear();
            var data = await JsonSerializer.DeserializeAsync<List<T>>(values, jsonSerializerOptions) ?? new List<T>();
            foreach (var item in data)
            {
                this.TryAdd(item.Id, item);
            }
        }
    }
}
