using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrustInnova.Application.DataStorage
{
    public interface IDataStorageService
    {
        ValueTask ClearAsync(CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<int> LengthAsync(CancellationToken cancellationToken = default(CancellationToken));

        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default(CancellationToken));
    }
}
