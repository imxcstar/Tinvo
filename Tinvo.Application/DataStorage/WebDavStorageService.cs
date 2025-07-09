using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tinvo.Abstractions.AIScheduler;
using WebDav;

namespace Tinvo.Application.DataStorage
{
    public class WebDavStorageService : IDataStorageService
    {
        private readonly IWebDavClient _client;
        private readonly string _baseUri;
        private readonly JsonSerializerOptions _serializerOptions;

        public WebDavStorageService(HttpClient httpClient, string baseUri, string userName, string password)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }
            if (string.IsNullOrWhiteSpace(baseUri))
            {
                throw new ArgumentNullException(nameof(baseUri));
            }

            _baseUri = baseUri.TrimEnd('/') + "/";

            httpClient.BaseAddress = new Uri(_baseUri);
            var byteArray = Encoding.ASCII.GetBytes($"{userName}:{password}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            _client = new WebDavClient(httpClient);

            CreateBaseDirectoryIfNotExists();

            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
        }

        private async void CreateBaseDirectoryIfNotExists()
        {
            var result = await _client.Propfind(_baseUri);
            if (result.IsSuccessful && result.Resources.Any())
            {
                return;
            }

            await _client.Mkcol(_baseUri);
        }

        private string GetItemUri(string key) => _baseUri + key;

        public async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            var response = await _client.Propfind(itemUri);
            return response.IsSuccessful && response.Resources.Count > 0;
        }

        public async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            var response = await _client.Propfind(_baseUri);
            if (!response.IsSuccessful)
            {
                return Enumerable.Empty<string>();
            }

            // Exclude the directory itself from the list of keys.
            return response.Resources
                .Where(r => r.Uri.TrimEnd('/') != _baseUri.TrimEnd('/'))
                .Select(r => Path.GetFileName(r.Uri.TrimEnd('/')));
        }

        public async ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            var keys = await KeysAsync(cancellationToken);
            return keys.Count();
        }

        public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var jsonData = await GetItemAsStringAsync(key, cancellationToken);
            return jsonData is null ? default : JsonSerializer.Deserialize<T>(jsonData, _serializerOptions);
        }

        public async ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            var response = await _client.GetRawFile(itemUri);
            if (!response.IsSuccessful)
            {
                return null;
            }

            using var reader = new StreamReader(response.Stream);
            return await reader.ReadToEndAsync();
        }

        public async ValueTask<byte[]?> GetItemAsBinaryAsync(string key, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            var response = await _client.GetRawFile(itemUri);
            if (!response.IsSuccessful)
            {
                return null;
            }

            using var memoryStream = new MemoryStream();
            await response.Stream.CopyToAsync(memoryStream, cancellationToken);
            return memoryStream.ToArray();
        }

        public async ValueTask<Stream?> GetItemAsStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            var response = await _client.GetRawFile(itemUri);
            return response.IsSuccessful ? response.Stream : null;
        }

        public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            var jsonData = JsonSerializer.Serialize(data, _serializerOptions);
            await SetItemAsStringAsync(key, jsonData, cancellationToken);
        }

        public async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            await SetItemAsStreamAsync(key, stream, cancellationToken: cancellationToken);
        }

        public async ValueTask SetItemAsBinaryAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream(data);
            await SetItemAsStreamAsync(key, stream, cancellationToken: cancellationToken);
        }

        public async ValueTask SetItemAsStreamAsync(string key, Stream data, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            await _client.PutFile(itemUri, data, "application/octet-stream");

            if (!leaveOpen)
            {
                data.Close();
            }
        }

        public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            var itemUri = GetItemUri(key);
            await _client.Delete(itemUri);
        }

        public async ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                await RemoveItemAsync(key, cancellationToken);
            }
        }

        public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            var keys = await KeysAsync(cancellationToken);
            await RemoveItemsAsync(keys, cancellationToken);
        }
    }
}
