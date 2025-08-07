using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tinvo.Application.DataStorage;

namespace Tinvo.Abstractions.DB
{
    /// <summary>
    /// 数据存储目录，记录着最新ID块的ID，是整个数据链的入口点。
    /// </summary>
    public class Catalog
    {
        public string? LatestBlockId { get; set; }
    }

    /// <summary>
    /// ID块，作为双向链表中的一个节点。
    /// 它只存储数据ID的集合，用于将大量数据ID分片管理。
    /// </summary>
    public class IdBlock
    {
        public required string Id { get; set; }
        public string? PreviousBlockId { get; set; }
        public string? NextBlockId { get; set; }

        /// <summary>
        /// 当前块中存储的数据ID集合。
        /// </summary>
        public List<string> DataIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// 实体记录，用于存储完整的业务实体对象以及必要的元数据。
    /// </summary>
    /// <typeparam name="T">存储的实体类型</typeparam>
    public class EntityRecord<T> where T : IDBEntity
    {
        /// <summary>
        /// 实体ID所属的ID块的ID。用于在删除时快速定位并更新ID块。
        /// </summary>
        public required string BlockId { get; set; }

        /// <summary>
        /// 完整的业务实体对象。
        /// </summary>
        public T? Data { get; set; }
    }

    /// <summary>
    /// 用于导入和导出数据库内容的辅助数据结构。
    /// </summary>
    public class LinkedDBExportData<T> where T : IDBEntity
    {
        public Catalog? Catalog { get; set; }
        public List<IdBlock> Blocks { get; set; } = new List<IdBlock>();
        public List<EntityRecord<T>> Entities { get; set; } = new List<EntityRecord<T>>();
    }

    /// <summary>
    /// 一个基于链式ID块和独立实体记录的数据库结构。
    /// 这种设计将ID分组管理与实体数据存储分离，以优化性能和结构清晰度。
    /// </summary>
    /// <typeparam name="T">实体类型，必须实现IDBEntity接口</typeparam>
    public class LinkedDB<T> : IDB<T> where T : IDBEntity
    {
        private IDataStorageService? _dataStorageService;
        private readonly IDataStorageServiceFactory _dataStorageServiceFactory;

        // 配置项
        private readonly string _entityName;
        private readonly int _maxBlockSize;

        // 数据库键名
        private readonly string _catalogKey;
        private readonly string _blockKeyPrefix;
        private readonly string _entityKeyPrefix;

        // 内存中的状态
        private Catalog? _catalog;
        private readonly ConcurrentDictionary<string, IdBlock> _loadedBlocks = new ConcurrentDictionary<string, IdBlock>();
        private static readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(1, 1);

        public LinkedDB(IDataStorageServiceFactory dataStorageServiceFactory, int maxBlockSize = 30)
        {
            _dataStorageServiceFactory = dataStorageServiceFactory;
            _maxBlockSize = maxBlockSize > 0 ? maxBlockSize : 30;

            _entityName = typeof(T).GetCustomAttributes<TableAttribute>(false).FirstOrDefault()?.Name ?? typeof(T).Name.Replace("Entity", "");
            _catalogKey = $"db_{_entityName}_catalog";
            _blockKeyPrefix = $"db_{_entityName}_block_";
            _entityKeyPrefix = $"db_{_entityName}_entity_";
        }

        public async Task InitAsync()
        {
            _dataStorageService = await _dataStorageServiceFactory.CreateAsync();
            _catalog = await _dataStorageService.GetItemAsync<Catalog>(_catalogKey);

            if (_catalog == null || string.IsNullOrEmpty(_catalog.LatestBlockId))
            {
                var firstBlock = CreateNewBlock(null);
                _catalog = new Catalog { LatestBlockId = firstBlock.Id };
                _loadedBlocks.TryAdd(firstBlock.Id, firstBlock);

                await _dataStorageService.SetItemAsync(GetBlockStorageKey(firstBlock.Id), firstBlock);
                await _dataStorageService.SetItemAsync(_catalogKey, _catalog);
            }
            else
            {
                await GetBlockByIdAsync(_catalog.LatestBlockId);
            }
        }

        public async Task AddAsync(T item)
        {
            TryCheckDatabaseIsInitialized();

            if (item == null || string.IsNullOrEmpty(item.Id))
            {
                throw new ArgumentNullException(nameof(item), "Item and its ID cannot be null.");
            }

            await _writeSemaphore.WaitAsync();
            try
            {
                var existingRecord = await _dataStorageService!.GetItemAsync<EntityRecord<T>>(GetEntityStorageKey(item.Id));
                if (existingRecord != null)
                {
                    throw new InvalidOperationException($"An item with ID '{item.Id}' already exists.");
                }

                var latestBlock = await GetBlockByIdAsync(_catalog!.LatestBlockId);
                IdBlock targetBlock;
                var saveTasks = new List<ValueTask>();

                if (latestBlock == null || latestBlock.DataIds.Count >= _maxBlockSize)
                {
                    var previousBlockId = latestBlock?.Id;
                    targetBlock = CreateNewBlock(previousBlockId);
                    _loadedBlocks.TryAdd(targetBlock.Id, targetBlock);

                    if (latestBlock != null)
                    {
                        latestBlock.NextBlockId = targetBlock.Id;
                        saveTasks.Add(_dataStorageService.SetItemAsync(GetBlockStorageKey(latestBlock.Id), latestBlock));
                    }

                    _catalog.LatestBlockId = targetBlock.Id;
                    saveTasks.Add(_dataStorageService.SetItemAsync(_catalogKey, _catalog));
                }
                else
                {
                    targetBlock = latestBlock;
                }

                targetBlock.DataIds.Add(item.Id);
                var newRecord = new EntityRecord<T> { BlockId = targetBlock.Id, Data = item };

                saveTasks.Add(_dataStorageService.SetItemAsync(GetBlockStorageKey(targetBlock.Id), targetBlock));
                saveTasks.Add(_dataStorageService.SetItemAsync(GetEntityStorageKey(item.Id), newRecord));

                await Task.WhenAll(saveTasks.Select(x => x.AsTask()));
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task<T?> GetByIdAsync(string id)
        {
            TryCheckDatabaseIsInitialized();

            if (string.IsNullOrEmpty(id)) return default;

            var record = await _dataStorageService!.GetItemAsync<EntityRecord<T>>(GetEntityStorageKey(id));
            if (record == null)
                return default;
            return record.Data;
        }

        public async Task<bool> UpdateAsync(T item)
        {
            TryCheckDatabaseIsInitialized();

            if (item == null || string.IsNullOrEmpty(item.Id)) return false;

            await _writeSemaphore.WaitAsync();
            try
            {
                var existingRecord = await _dataStorageService!.GetItemAsync<EntityRecord<T>>(GetEntityStorageKey(item.Id));
                if (existingRecord == null) return false;

                var updatedRecord = new EntityRecord<T> { BlockId = existingRecord.BlockId, Data = item };
                await _dataStorageService.SetItemAsync(GetEntityStorageKey(item.Id), updatedRecord);
                return true;
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            TryCheckDatabaseIsInitialized();

            if (string.IsNullOrEmpty(id)) return false;

            await _writeSemaphore.WaitAsync();
            try
            {
                var recordToDelete = await _dataStorageService!.GetItemAsync<EntityRecord<T>>(GetEntityStorageKey(id));
                if (recordToDelete == null) return false;

                var targetBlock = await GetBlockByIdAsync(recordToDelete.BlockId);
                if (targetBlock == null || !targetBlock.DataIds.Remove(id))
                {
                    await _dataStorageService.RemoveItemAsync(GetEntityStorageKey(id));
                    return false;
                }

                var updateTasks = new List<ValueTask>
            {
                _dataStorageService.RemoveItemAsync(GetEntityStorageKey(id))
            };

                if (targetBlock.DataIds.Count == 0)
                {
                    if (targetBlock.PreviousBlockId != null)
                    {
                        var prevBlock = await GetBlockByIdAsync(targetBlock.PreviousBlockId);
                        if (prevBlock != null)
                        {
                            prevBlock.NextBlockId = targetBlock.NextBlockId;
                            updateTasks.Add(_dataStorageService.SetItemAsync(GetBlockStorageKey(prevBlock.Id), prevBlock));
                        }
                    }
                    if (targetBlock.NextBlockId != null)
                    {
                        var nextBlock = await GetBlockByIdAsync(targetBlock.NextBlockId);
                        if (nextBlock != null)
                        {
                            nextBlock.PreviousBlockId = targetBlock.PreviousBlockId;
                            updateTasks.Add(_dataStorageService.SetItemAsync(GetBlockStorageKey(nextBlock.Id), nextBlock));
                        }
                    }
                    if (_catalog!.LatestBlockId == targetBlock.Id)
                    {
                        _catalog.LatestBlockId = targetBlock.PreviousBlockId;
                        updateTasks.Add(_dataStorageService.SetItemAsync(_catalogKey, _catalog));
                    }

                    _loadedBlocks.TryRemove(targetBlock.Id, out _);
                    updateTasks.Add(_dataStorageService.RemoveItemAsync(GetBlockStorageKey(targetBlock.Id)));
                }
                else
                {
                    updateTasks.Add(_dataStorageService.SetItemAsync(GetBlockStorageKey(targetBlock.Id), targetBlock));
                }

                await Task.WhenAll(updateTasks.Select(x => x.AsTask()));
                return true;
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async IAsyncEnumerable<T> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            TryCheckDatabaseIsInitialized();

            var currentBlockId = _catalog!.LatestBlockId;
            while (currentBlockId != null && !cancellationToken.IsCancellationRequested)
            {
                var currentBlock = await GetBlockByIdAsync(currentBlockId);
                if (currentBlock != null)
                {
                    foreach (var id in currentBlock.DataIds)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var item = await GetByIdAsync(id);
                        if (item != null)
                        {
                            yield return item;
                        }
                    }
                    currentBlockId = currentBlock.PreviousBlockId;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 将整个数据库内容导出为JSON字符串。
        /// 此操作会创建一个数据快照，确保导出数据的一致性。
        /// </summary>
        /// <param name="jsonSerializerOptions">JSON序列化选项。</param>
        /// <returns>包含数据库所有数据的JSON字符串。</returns>
        public async Task<string> ExportJsonTextAsync(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            TryCheckDatabaseIsInitialized();

            await _writeSemaphore.WaitAsync();
            try
            {
                var exportData = new LinkedDBExportData<T> { Catalog = _catalog };

                var currentBlockId = _catalog!.LatestBlockId;
                while (currentBlockId != null)
                {
                    var currentBlock = await GetBlockByIdAsync(currentBlockId);
                    if (currentBlock != null)
                    {
                        exportData.Blocks.Add(currentBlock);
                        foreach (var dataId in currentBlock.DataIds)
                        {
                            var entityRecord = await _dataStorageService!.GetItemAsync<EntityRecord<T>>(GetEntityStorageKey(dataId));
                            if (entityRecord != null)
                            {
                                exportData.Entities.Add(entityRecord);
                            }
                        }
                        currentBlockId = currentBlock.PreviousBlockId;
                    }
                    else
                    {
                        break;
                    }
                }

                return JsonSerializer.Serialize(exportData, jsonSerializerOptions);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        /// <summary>
        /// 从流中导入数据。此操作会先【清空】所有现有数据，然后完全替换为导入的数据。
        /// </summary>
        /// <param name="stream">包含JSON数据的流。</param>
        /// <param name="jsonSerializerOptions">JSON序列化选项。</param>
        public async Task ImportAsync(Stream stream, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            await _writeSemaphore.WaitAsync();
            try
            {
                var importData = await JsonSerializer.DeserializeAsync<LinkedDBExportData<T>>(stream, jsonSerializerOptions);
                if (importData?.Catalog == null || importData.Blocks == null || importData.Entities == null)
                {
                    throw new JsonException("Import stream did not contain valid data or was empty.");
                }

                await ClearAllDataAsync();

                if (string.IsNullOrEmpty(importData.Catalog.LatestBlockId))
                {
                    await InitAsync();
                    return;
                }

                var saveTasks = new List<ValueTask>();

                foreach (var entity in importData.Entities)
                {
                    if (entity.Data != null)
                    {
                        saveTasks.Add(_dataStorageService!.SetItemAsync(GetEntityStorageKey(entity.Data.Id), entity));
                    }
                }

                foreach (var block in importData.Blocks)
                {
                    saveTasks.Add(_dataStorageService!.SetItemAsync(GetBlockStorageKey(block.Id), block));
                }

                saveTasks.Add(_dataStorageService!.SetItemAsync(_catalogKey, importData.Catalog));

                await Task.WhenAll(saveTasks.Select(x => x.AsTask()));

                _catalog = importData.Catalog;
                _loadedBlocks.Clear();
                await GetBlockByIdAsync(_catalog.LatestBlockId);
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task ClearAllDataAsync()
        {
            TryCheckDatabaseIsInitialized();

            var removeTasks = new List<ValueTask>();
            var currentBlockId = _catalog.LatestBlockId;
            var processedBlockIds = new HashSet<string>();

            while (currentBlockId != null && processedBlockIds.Add(currentBlockId))
            {
                var currentBlock = await GetBlockByIdAsync(currentBlockId);
                if (currentBlock != null)
                {
                    foreach (var dataId in currentBlock.DataIds)
                    {
                        removeTasks.Add(_dataStorageService!.RemoveItemAsync(GetEntityStorageKey(dataId)));
                    }
                    removeTasks.Add(_dataStorageService!.RemoveItemAsync(GetBlockStorageKey(currentBlock.Id)));
                    currentBlockId = currentBlock.PreviousBlockId;
                }
                else
                {
                    break;
                }
            }

            removeTasks.Add(_dataStorageService!.RemoveItemAsync(_catalogKey));
            await Task.WhenAll(removeTasks.Select(x => x.AsTask()));

            _catalog = null;
            _loadedBlocks.Clear();
        }

        #region Helper Methods

        private string GetBlockStorageKey(string blockId) => $"{_blockKeyPrefix}{blockId}";
        private string GetEntityStorageKey(string entityId) => $"{_entityKeyPrefix}{entityId}";

        private IdBlock CreateNewBlock(string? previousBlockId)
        {
            return new IdBlock
            {
                Id = Guid.NewGuid().ToString("N"),
                PreviousBlockId = previousBlockId,
                NextBlockId = null
            };
        }
        
        private async Task<IdBlock?> GetBlockByIdAsync(string? blockId)
        {
            if (string.IsNullOrEmpty(blockId)) return null;
            if (_loadedBlocks.TryGetValue(blockId, out var block)) return block;

            var loadedBlock = await _dataStorageService!.GetItemAsync<IdBlock>(GetBlockStorageKey(blockId));
            if (loadedBlock != null)
            {
                _loadedBlocks.TryAdd(blockId, loadedBlock);
            }
            return loadedBlock;
        }

        private void TryCheckDatabaseIsInitialized()
        {
            if (_dataStorageService == null || _catalog == null || string.IsNullOrEmpty(_catalog.LatestBlockId))
            {
                throw new InvalidOperationException("Database has not been initialized. Call InitAsync() first.");
            }
        }

        #endregion
    }
}
