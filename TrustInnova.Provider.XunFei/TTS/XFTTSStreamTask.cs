using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrustInnova.Abstractions.TTS;
using TrustInnova.Provider.XunFei.API;
using TrustInnova.Utils.Extend;
using Microsoft.Extensions.Configuration;
using TrustInnova.Abstractions;
using System.ComponentModel;

namespace TrustInnova.Provider.XunFei.TTS
{
    [TypeMetadataDisplayName("语音合成配置")]
    public class TTSConfig_XunFei
    {
        [Description("地址")]
        public string HostURL { get; set; } = null!;
        public string AppId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string ApiSecret { get; set; } = null!;
    }

    [ProviderTask("XFTTS", "讯飞语音合成")]
    public class XFTTSStreamTask : TTSStreamTask
    {
        private readonly TTSConfig_XunFei _config;

        public override bool SupportStream { get; } = true;

        private ClientWebSocket _client { get; set; } = new ClientWebSocket();
        private readonly ILogger _logger;


        public XFTTSStreamTask(TTSConfig_XunFei config)
        {
            _config = config;
            _logger = Log.ForContext<XFTTSStreamTask>();
        }

        public override async Task CloseAsync()
        {
            try
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                _client = new ClientWebSocket();
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

        public override async IAsyncEnumerable<TTSTaskReceiveMessageResponse> Listening([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = new byte[1024 * 1024 * 10];
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    await CloseAsync();
                    break;
                }

                var currentTime = DateTime.Now;
                var timeDifference = currentTime - HeartBeatTime;

                if (timeDifference.TotalSeconds <= 10)
                {
                    if (_client.State == WebSocketState.None || _client.State == WebSocketState.Closed || _client.State == WebSocketState.Aborted)
                    {
                        await ConnectAsync();
                        await Task.Delay(500);
                    }
                    while (_client.State == WebSocketState.Open)
                    {
                        var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        _logger.Debug($"XFTTS ReceiveMessages: {DateTime.Now}");
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseAsync();
                            break;
                        }
                        else
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            _logger.Debug($"XFTTS RawMessage: {message}");
                            dynamic? msg = JsonConvert.DeserializeObject(message);
                            if (msg == null)
                                continue;
                            if (msg.code != 0)
                            {
                                _logger.Error($"error => {msg.message},sid => {msg.sid}");
                                yield return new TTSTaskReceiveMessageResponse()
                                {
                                    Success = false,
                                    Msg = msg.message?.ToString(),
                                    End = true
                                };
                                continue;
                            }
                            var data = msg.data;
                            if (data == null)
                                continue;
                            var audio = data.audio?.ToString();
                            if (audio == null)
                                continue;
                            yield return new TTSTaskReceiveMessageResponse()
                            {
                                Success = true,
                                Data = Convert.FromBase64String(audio),
                                End = data.status == 2
                            };
                            if (data.status == 2)
                            {
                                await CloseAsync();
                                if (EnableStream)
                                    break;
                                else
                                    yield break;
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
                await Task.Delay(1);
            }
        }

        public override async Task SynthesizeAsync(string value, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            HeartBeatTime = DateTime.Now;

            dynamic data = new JObject();
            data.common = new JObject
            {
                {"app_id" ,_config.AppId }
            };
            data.business = new JObject
            {
                { "aue", metadata.GetValueOrDefaultTypeValue("xf.aue", "raw") },
                { "sfl", metadata.GetValueOrDefaultTypeValue("xf.sfl",0) },
                { "auf", metadata.GetValueOrDefaultTypeValue("xf.auf","audio/L16;rate=16000") },
                { "vcn", metadata.GetValueOrDefaultTypeValue("xf.vcn","xiaoyan") },
                { "speed", metadata.GetValueOrDefaultTypeValue("xf.speed",50) },
                { "volume", metadata.GetValueOrDefaultTypeValue("xf.volume",50) },
                { "pitch", metadata.GetValueOrDefaultTypeValue("xf.pitch",50) },
                { "bgs", metadata.GetValueOrDefaultTypeValue("xf.bgs",0) },
                { "tte", metadata.GetValueOrDefaultTypeValue("xf.tte","UTF8") },
                { "reg", metadata.GetValueOrDefaultTypeValue("xf.reg","0") },
                { "rdn", metadata.GetValueOrDefaultTypeValue("xf.rdn","0") },
            };
            data.data = new JObject
            {
                { "status",2 },
                { "text",Convert.ToBase64String(Encoding.UTF8.GetBytes(value))},
            };
            await _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data.ToString())), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
