using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Application.DataStorage
{
    public class FileStorageService : IDataStorageService
    {
        private readonly string _storageDirectory;
        private readonly JsonSerializerOptions _serializerOptions;

        public FileStorageService(string storageDirectory)
        {
            _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
            }
            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
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
                return JsonSerializer.Deserialize<T>(jsonData, _serializerOptions);
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
            string jsonData = JsonSerializer.Serialize(data, _serializerOptions);
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

        public async ValueTask SetItemAsStreamAsync(string key, Stream data, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            using var file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            await data.CopyToAsync(file);
            file.Close();
            if (!leaveOpen)
                data.Close();
        }

        public ValueTask SetItemAsBinaryAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            File.WriteAllBytes(filePath, data);
            return ValueTask.CompletedTask;
        }

        public ValueTask<Stream?> GetItemAsStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            if (!File.Exists(filePath))
                return ValueTask.FromResult<Stream?>(null);
            return ValueTask.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite));
        }

        public ValueTask<byte[]?> GetItemAsBinaryAsync(string key, CancellationToken cancellationToken = default)
        {
            string filePath = GetFilePath(key);
            if (!File.Exists(filePath))
                return ValueTask.FromResult<byte[]?>(null);
            return ValueTask.FromResult<byte[]?>(File.ReadAllBytes(filePath));
        }
    }
}
