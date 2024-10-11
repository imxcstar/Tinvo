using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.ASR;
using TrustInnova.Provider.XunFei.API;
using TrustInnova.Utils.Extend;
using TrustInnova.Utils.FFMpeg;

namespace TrustInnova.Provider.XunFei.ASR
{
    [TypeMetadataDisplayName("语音识别配置")]
    public class ASRConfig_XunFei
    {
        [Description("地址")]
        public string HostURL { get; set; } = null!;
        public string AppId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
    }

    [ProviderTask("XFASR", "讯飞语音识别")]
    public class XFASRStreamTask : ASRStreamTask
    {
        private readonly ASRConfig_XunFei _config;
        private readonly FFMpegService _ffmpegService;
        private readonly ILogger _logger;

        public override bool SupportStream { get; } = true;

        private ClientWebSocket _client { get; set; } = new ClientWebSocket();

        public XFASRStreamTask(ASRConfig_XunFei config, FFMpegService ffmpegService)
        {
            _config = config;
            _ffmpegService = ffmpegService;
            _logger = Log.ForContext<XFASRStreamTask>();
        }

        public override async Task CloseAsync()
        {
            try
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            catch (Exception)
            {
            }
        }

        public override async Task ConnectAsync()
        {
            var url = XFUtils.GetAuth(_config.HostURL, _config.ApiKey, _config.ApiSecret);
            await _client.ConnectAsync(new Uri(url), CancellationToken.None);
        }

        public override async IAsyncEnumerable<ASRTaskReceiveMessageResponse> ReceiveMessages([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = new byte[1024 * 1024 * 10];
            var chunk = new StringBuilder();
            var ret = new StringBuilder();
            while (_client.State == WebSocketState.Open)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await CloseAsync();
                    break;
                }
                var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                _logger.Debug($"XFASR ReceiveMessages: {DateTime.Now}");
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseAsync();
                    break;
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.Debug($"XFASR RawMessage: {message}");
                    dynamic? msg = JsonConvert.DeserializeObject(message);
                    if (msg == null)
                        continue;
                    if (msg.code != 0)
                    {
                        _logger.Error($"error => {msg.message},sid => {msg.sid}");
                        yield return new ASRTaskReceiveMessageResponse()
                        {
                            Success = false,
                            Msg = msg.message?.ToString(),
                            End = true
                        };
                        continue;
                    }
                    var ws = msg.data.result.ws;
                    if (ws == null)
                        continue;
                    var strBuilder = new StringBuilder();
                    foreach (var item in ws)
                    {
                        var tmsg = item.cw[0].w?.ToString();
                        if (!string.IsNullOrEmpty(tmsg))
                            strBuilder.Append(tmsg);
                    }
                    if (msg.data.result.pgs == "rpl")
                    {
                        ret.Clear();
                        ret.Append(chunk.ToString());
                    }
                    else
                    {
                        chunk.Append(ret.ToString());
                    }
                    ret.Append(strBuilder);
                    if (msg.data.status == 2)
                    {
                        yield return new ASRTaskReceiveMessageResponse()
                        {
                            Success = true,
                            Msg = ret.ToString(),
                            End = true
                        };
                        await CloseAsync();
                        yield break;
                    }
                    else
                    {
                        if (EnableStream)
                            yield return new ASRTaskReceiveMessageResponse()
                            {
                                Success = true,
                                Msg = ret.ToString(),
                                End = false
                            };
                    }
                }
            }
        }

        private async Task SendAsync(byte[] data, ASRTaskAudioType type, ASRFrameType frameType, Dictionary<string, object>? metadata = null)
        {
            var tmetadata = metadata ?? new Dictionary<string, object>();
            var encoding = type switch
            {
                ASRTaskAudioType.MP3 => "lame",
                _ => "raw"
            };

            object? format = "audio/L16;rate=16000";

            if (type != ASRTaskAudioType.PCM && type != ASRTaskAudioType.MP3)
            {
                var (PcmData, Output, Error) = await _ffmpegService.ToPCMAsync(data, type.ToString().ToLower(), 16000, 1);
                data = PcmData;
            }
            else if (!tmetadata.TryGetValue("xf.format", out format))
                throw new Exception("缺少必要元数据xf.format");

            dynamic frame = new JObject();
            switch (frameType)
            {
                case ASRFrameType.FirstFrame:
                    frame.common = new JObject
                        {
                            {"app_id" ,_config.AppId }
                        };
                    frame.business = new JObject
                        {
                            { "language",tmetadata.GetValueOrDefaultTypeValue("xf.language", "zh_cn") },
                            { "vad_eos",tmetadata.GetValueOrDefaultTypeValue("xf.vad_eos", 3000) },
                            { "domain",tmetadata.GetValueOrDefaultTypeValue("xf.domain", "iat") },
                            { "accent",tmetadata.GetValueOrDefaultTypeValue("xf.accent", "mandarin")},
                            { "dwa",tmetadata.GetValueOrDefaultTypeValue("xf.dwa", "wpgs")},
                            { "ptt",tmetadata.GetValueOrDefaultTypeValue("xf.ptt", 1)}
                        };
                    frame.data = new JObject
                        {
                            { "status",(int)frameType },
                            { "format",(string)format},
                            { "encoding",(string)encoding },
                            { "audio",Convert.ToBase64String(data)}
                        };
                    break;
                default:
                    frame.data = new JObject
                    {
                            { "status",(int)frameType },
                            { "format",(string)format},
                            { "encoding",(string)encoding },
                            { "audio",Convert.ToBase64String(data)}
                        };
                    break;
            }
            await _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(frame.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public override async Task SendAsync(Stream data, ASRTaskAudioType type, ASRFrameType? frameType = null, Dictionary<string, object>? metadata = null, bool autoSplitFrame = false, CancellationToken cancellationToken = default)
        {
            if (autoSplitFrame)
            {
                var tmetadata = metadata ?? new Dictionary<string, object>();
                frameType = ASRFrameType.FirstFrame;

                while (true)
                {
                    byte[] buffer = new byte[1280];
                    int r = data.Read(buffer, 0, buffer.Length);
                    if (r < 1280)
                    {
                        frameType = ASRFrameType.LastFrame;
                    }
                    await SendAsync(buffer, type, frameType.Value, tmetadata);
                    if (frameType == ASRFrameType.FirstFrame)
                        frameType = ASRFrameType.ContinueFrame;
                    if (r < 1280)
                        break;
                }
            }
            else
            {
                if (frameType == null)
                    frameType = ASRFrameType.LastFrame;

                var buffer = new byte[data.Length];
                data.Read(buffer, 0, buffer.Length);
                await SendAsync(buffer, type, frameType.Value, metadata);
            }
        }
    }
}
