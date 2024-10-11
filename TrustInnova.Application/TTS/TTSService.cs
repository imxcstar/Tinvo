using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.TTS;
using TrustInnova.Provider.XunFei.TTS;

namespace TrustInnova.Application.TTS
{
    public class TTSService
    {
        private readonly TTSTaskCache _taskCache;

        public TTSService(TTSTaskCache taskCache)
        {
            _taskCache = taskCache;
        }

        public async Task<ITTSTask?> NewTaskAsync(TTSTaskType taskType, string userId, object config)
        {
            ITTSTask ret;
            switch (taskType)
            {
                case TTSTaskType.XunFei:
                    ret = new XFTTSStreamTask((TTSConfig_XunFei)config);
                    break;
                default:
                    throw new Exception("不支持的任务类型");
            }
            if (!_taskCache.Caches.TryAdd(userId, ret))
                return null;
            await ret.ConnectAsync();
            return ret;
        }

        public async Task DelTaskAsync(string userId)
        {
            _taskCache.Caches.TryRemove(userId, out var info);
            if (info != null)
                await info.CloseAsync();
        }

        public ITTSTask? GetTask(string userId)
        {
            if (!_taskCache.Caches.TryGetValue(userId, out var info))
                return null;
            return info;
        }

        public bool CheckTaskExist(string userId)
        {
            return _taskCache.Caches.ContainsKey(userId);
        }
    }
}
