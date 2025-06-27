using Microsoft.JSInterop;
using System.Text.Json;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Application.DataStorage;
using static MudBlazor.Colors;

namespace Tinvo.Services
{
    public class LocalForageService : IDataStorageService
    {
        private readonly IJSRuntime _js;
        private readonly JsonSerializerOptions _serializerOptions;

        public LocalForageService(IJSRuntime js)
        {
            _js = js;
            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.Converters.Add(new IAIChatHandleMessageConverter());
        }

        public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            await _js.InvokeVoidAsync("localForageActions.clear");
        }

        public async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _js.InvokeAsync<bool>("localForageActions.containKey", key);
        }

        public async ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _js.InvokeAsync<string?>("localForageActions.getItem", key);
        }

        public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var data = await GetItemAsStringAsync(key, cancellationToken);
            if (string.IsNullOrWhiteSpace(data))
                return default;
            return JsonSerializer.Deserialize<T?>(data, _serializerOptions);
        }

        public async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            return await _js.InvokeAsync<IEnumerable<string>>("localForageActions.keys");
        }

        public async ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            return await _js.InvokeAsync<int>("localForageActions.length");
        }

        public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            await _js.InvokeVoidAsync("localForageActions.removeItem", key);
        }

        public async ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                await _js.InvokeVoidAsync("localForageActions.removeItem", key);
            }
        }

        public async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            await _js.InvokeVoidAsync("localForageActions.setItem", key, data);
        }

        public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            await SetItemAsStringAsync(key, JsonSerializer.Serialize(data, _serializerOptions));
        }

        public async ValueTask<byte[]?> GetItemAsBinaryAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _js.InvokeAsync<byte[]?>("localForageActions.getItem", key);
        }

        public async ValueTask<Stream?> GetItemAsStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            var ret = await _js.InvokeAsync<byte[]?>("localForageActions.getItem", key);
            return ret == null ? null : new MemoryStream(ret);
        }

        public async ValueTask SetItemAsBinaryAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            await _js.InvokeVoidAsync("localForageActions.setItem", key, data);
        }

        public async ValueTask SetItemAsStreamAsync(string key, Stream data, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
            var memoryStream = new MemoryStream();
            await data.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            await _js.InvokeVoidAsync("localForageActions.setItem", key, memoryStream.ToArray());
        }
    }

}
