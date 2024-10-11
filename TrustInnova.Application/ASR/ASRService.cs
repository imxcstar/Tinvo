using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.ASR;
using TrustInnova.Provider.Baidu.ASR;
using TrustInnova.Provider.XunFei.ASR;
using TrustInnova.Utils.FFMpeg;

namespace TrustInnova.Application.ASR
{
    public class ASRService
    {
        private readonly ASRTaskCache _taskCache;
        private readonly FFMpegService _ffmpegService;

        public ASRService(ASRTaskCache taskCache, FFMpegService ffmpegService)
        {
            _taskCache = taskCache;
            _ffmpegService = ffmpegService;
        }

        public async Task<IASRTask?> NewTaskAsync(ASRTaskType taskType, string userId,object config)
        {
            IASRTask ret;
            switch (taskType)
            {
                case ASRTaskType.XunFei:
                    ret = new XFASRStreamTask((ASRConfig_XunFei)config, _ffmpegService);
                    break;
                case ASRTaskType.Baidu:
                    ret = new BaiduASRTask((ASRConfig_BaiDu)config, _ffmpegService);
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

        public IASRTask? GetTask(string userId)
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
