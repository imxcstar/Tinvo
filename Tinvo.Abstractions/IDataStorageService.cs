using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinvo.Application.DataStorage
{
    public interface IDataStorageServiceFactory
    {
        public IDataStorageService Create();

        public Task<IDataStorageService> CreateAsync();

        public Task<DataStorageSettingInfo> GetConfigAsync();

        public Task SaveConfigAsync(DataStorageSettingInfo setting);
    }


    public class DataStorageSettingInfo
    {
        public DataStorageType Type { get; set; }

        public string? WebDavURI { get; set; }

        public string? WebDavUserName { get; set; }

        public string? WebDavPassword { get; set; }
    }

    public enum DataStorageType
    {
        Local,
        WebDav
    }

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

        ValueTask SetItemAsStreamAsync(string key, Stream data, bool leaveOpen = false, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask SetItemAsBinaryAsync(string key, byte[] data, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<Stream?> GetItemAsStreamAsync(string key, CancellationToken cancellationToken = default(CancellationToken));

        ValueTask<byte[]?> GetItemAsBinaryAsync(string key, CancellationToken cancellationToken = default(CancellationToken));
    }
}
