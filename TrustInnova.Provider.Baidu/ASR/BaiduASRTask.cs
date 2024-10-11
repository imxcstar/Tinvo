using Baidu.Aip.Speech;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.ASR;
using TrustInnova.Utils.Extend;
using TrustInnova.Utils.FFMpeg;

namespace TrustInnova.Provider.Baidu.ASR
{
    [TypeMetadataDisplayName("语音识别配置")]
    public class ASRConfig_BaiDu
    {
        public string AppID { get; set; } = null!;
        public string API_KEY { get; set; } = null!;
        public string SECRET_KEY { get; set; } = null!;
    }

    [ProviderTask("BaiduASR", "百度语音识别")]
    public class BaiduASRTask : ASRStreamTask
    {
        private readonly ASRConfig_BaiDu _config;
        private readonly FFMpegService _ffmpegService;
        private readonly ILogger _logger;

        private ConcurrentQueue<ASRTaskReceiveMessageResponse> _resultQueue;
        private List<byte[]> _dataCache;

        public override bool SupportStream { get; } = false;

        public BaiduASRTask(ASRConfig_BaiDu config, FFMpegService ffmpegService)
        {
            _config = config;
            _resultQueue = new ConcurrentQueue<ASRTaskReceiveMessageResponse>();
            _ffmpegService = ffmpegService;
            _dataCache = new List<byte[]>();
            _logger = Log.ForContext<BaiduASRTask>();
        }

        public override async IAsyncEnumerable<ASRTaskReceiveMessageResponse> ReceiveMessages([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var currentTime = DateTime.Now;
                var timeDifference = currentTime - HeartBeatTime;

                if (timeDifference.TotalSeconds <= 3)
                {
                    if (!_resultQueue.IsEmpty)
                    {
                        if (!_resultQueue.TryDequeue(out var result))
                            yield return new ASRTaskReceiveMessageResponse()
                            {
                                Msg = "数据出列错误",
                                Success = false,
                                End = true
                            };
                        else
                            yield return result;
                    }
                }
                else
                {
                    break;
                }
                await Task.Delay(1);
            }
        }

        public override async Task SendAsync(Stream data, ASRTaskAudioType type, ASRFrameType? frameType = null, Dictionary<string, object>? metadata = null, bool autoSplitSend = false, CancellationToken cancellationToken = default)
        {
            HeartBeatTime = DateTime.Now;

            // 文件后缀 pcm/wav/amr 格式 极速版额外支持m4a 格式
            var client = new Asr(_config.AppID, _config.API_KEY, _config.SECRET_KEY)
            {
                Timeout = metadata.GetValueOrDefaultTypeValue("bd.timeout", 1200000)// 若语音较长，建议设置更大的超时时间. ms
            };

            var rate = metadata.GetValueOrDefaultTypeValue("bd.rate", 16000);

            var buffer = new byte[data.Length];
            data.Read(buffer, 0, buffer.Length);

            if (type != ASRTaskAudioType.PCM)
            {
                var (PcmData, Output, Error) = await _ffmpegService.ToPCMAsync(buffer, type.ToString().ToLower(), rate, 1);
                buffer = PcmData;
            }

            _dataCache.Add(buffer);

            if (frameType == null || frameType == ASRFrameType.LastFrame)
            {
                var result = client.Recognize(_dataCache.SelectMany(x => x).ToArray(), "pcm", rate);
                _dataCache.Clear();
                if (result?["err_no"]?.ToString() == "0")
                {
                    _resultQueue.Enqueue(new ASRTaskReceiveMessageResponse()
                    {
                        Msg = result["result"]?.ToObject<List<string>>()?.FirstOrDefault(),
                        Success = true,
                        End = true
                    });
                }
                else
                {
                    _logger.Error("Baidu ASR error: " + result?["err_msg"]?.ToString());
                    _resultQueue.Enqueue(new ASRTaskReceiveMessageResponse()
                    {
                        Msg = result?["err_msg"]?.ToString(),
                        Success = false,
                        End = true
                    });
                }
            }
        }
    }
}
