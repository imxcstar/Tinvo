using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TrustInnova.Application.DataStorage
{
    public class FileStorageService : IDataStorageService
    {
        private readonly string _storageDirectory;

        public FileStorageService(string storageDirectory)
        {
            _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
        }

        public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            var files = Directory.GetFiles(_storageDirectory);
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                string jsonData = await File.ReadAllTextAsync(filePath, cancellationToken);
                return JsonSerializer.Deserialize<T>(jsonData);
            }
            return default;
        }

        public async ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath, cancellationToken);
            }
            return null;
        }

        public async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            var files = Directory.GetFiles(_storageDirectory);
            return files.Select(Path.GetFileName);
        }

        public async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        public async ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            var files = Directory.GetFiles(_storageDirectory);
            return files.Length;
        }

        public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public async ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                string filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }

        public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            string jsonData = JsonSerializer.Serialize(data);
            string filePath = GetFilePath(key);
            await File.WriteAllTextAsync(filePath, jsonData, cancellationToken);
        }

        public async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            await File.WriteAllTextAsync(filePath, data, cancellationToken);
        }

        private string GetFilePath(string key)
        {
            return Path.Combine(_storageDirectory, key);
        }
    }
}
