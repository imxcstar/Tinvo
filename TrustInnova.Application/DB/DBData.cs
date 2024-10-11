using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrustInnova.Application.DataStorage;

namespace TrustInnova.Application.DB
{
    public class DBData<T> : ConcurrentDictionary<string, T> where T : IDBEntity
    {
        private readonly IDataStorageService _dataStorageService;
        private readonly string _name;

        public DBData(IDataStorageService dataStorageService)
        {
            _dataStorageService = dataStorageService;
            _name = typeof(T).GetCustomAttributes<TableAttribute>(false).FirstOrDefault()?.Name ?? typeof(T).Name.Replace("Entity", "");
            _name = $"db_{_name}";
        }

        public async Task InitAsync()
        {
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
    }
}
