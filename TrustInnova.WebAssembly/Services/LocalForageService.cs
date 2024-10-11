using Microsoft.JSInterop;
using System.Text.Json;
using TrustInnova.Application.DataStorage;

namespace TrustInnova.Services
{
    public class LocalForageService : IDataStorageService
    {
        private readonly IJSRuntime _js;

        public LocalForageService(IJSRuntime js)
        {
            _js = js;
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
            return await _js.InvokeAsync<T?>("localForageActions.getItem", key);
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
            await _js.InvokeVoidAsync("localForageActions.setItem", key, data);
        }
    }

}
