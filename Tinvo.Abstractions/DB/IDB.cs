using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tinvo.Abstractions.DB
{
    public interface IDB<T>
    {
        public Task InitAsync();
        public Task AddAsync(T item);
        public Task<T?> GetByIdAsync(string id);
        public Task<bool> UpdateAsync(T item);
        public Task<bool> DeleteAsync(string id);
        public IAsyncEnumerable<T> GetAllAsync(CancellationToken cancellationToken = default);
        public Task<string> ExportJsonTextAsync(JsonSerializerOptions? jsonSerializerOptions = null);
        public Task ImportAsync(Stream stream, JsonSerializerOptions? jsonSerializerOptions = null);
    }
}
