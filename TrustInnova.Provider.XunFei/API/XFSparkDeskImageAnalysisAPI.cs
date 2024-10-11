using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Provider.XunFei.API;

[TypeMetadataDisplayName("图片识别配置")]
public class XFSparkDeskImageAnalysisAPIConfig
{
    [Description("地址")]
    public string HostURL { get; set; } = null!;
    public string AppId { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ApiSecret { get; set; } = null!;
}

internal class XFSparkDeskImageAnalysisAPI
{
    private readonly XFSparkDeskImageAnalysisAPIConfig _config;
    private ClientWebSocket _client { get; set; }
    private readonly ILogger _logger;

    private byte[] _buffer = new byte[1024 * 1024 * 10];

    public XFSparkDeskImageAnalysisAPI(XFSparkDeskImageAnalysisAPIConfig config)
    {
        NewClient();
        _config = config;
        _logger = Log.ForContext<XFSparkDeskImageAnalysisAPI>();
    }

    private void NewClient()
    {
        _client = new ClientWebSocket();
    }

    private async Task CloseAsync()
    {
        try
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            NewClient();
        }
        catch (Exception)
        {
        }
    }

    private async Task ConnectAsync()
    {
        var url = XFUtils.GetAuth(_config.HostURL, _config.ApiKey, _config.ApiSecret);
        await _client.ConnectAsync(new Uri(url), CancellationToken.None);
    }

    private async Task Send(XFSparkDeskImageAPIRequest request, CancellationToken cancellationToken = default)
    {
        var data = new
        {
            header = new
            {
                app_id = _config.AppId
            },
            parameter = new
            {
                chat = new
                {
                    domain = request.Domain,
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens,
                    top_k = request.TopK,
                    auditing = request.Auditing
                },
            },
            payload = new
            {
                message = new
                {
                    text = request.Messages.Select(x => new
                    {
                        role = x.Role,
                        content = x.Content,
                        content_type = x.ContentType
                    }).ToList()
                }
            }
        };
        var sendStr = JsonSerializer.Serialize(data, XFUtils.JsonSerializerOptions);
        _logger.Debug("XFSparkDeskImageAPI Send(RAW): {value}", sendStr);
        await _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(sendStr)), WebSocketMessageType.Text, true, cancellationToken);
    }

    public async IAsyncEnumerable<XFSparkDeskImageAPIResponse> SendImageChat(XFSparkDeskImageAPIRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await ConnectAsync();
        await Send(request, cancellationToken);
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await CloseAsync();
                break;
            }

            if (_client.State == WebSocketState.Connecting)
            {
                await Task.Delay(1, cancellationToken);
                continue;
            }

            if (_client.State != WebSocketState.Open)
            {
                await CloseAsync();
                break;
            }

            var result = await _client.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken);
            _logger.Debug($"XFSparkDeskImage ReceiveMessages: {DateTime.Now}");
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await CloseAsync();
                break;
            }
            else
            {
                var message = Encoding.UTF8.GetString(_buffer, 0, result.Count);
                _logger.Debug($"XFSparkDeskImage RawMessage: {message}");
                var msg = JsonSerializer.Deserialize<XFSparkDeskImageAPIResponse>(message);
                if (msg == null || msg.Header == null || msg.Payload == null)
                {
                    _logger.Error($"XFSparkDeskImage RawMessage Deserialize Error: {message}");
                    throw new Exception(msg?.Header?.Message ?? "数据反序列化错误");
                }
                if (msg.Header.Code != 0)
                {
                    _logger.Error($"error => {msg.Header.Message},sid => {msg.Header.Sid}");
                    throw new Exception(msg.Header.Message);
                }
                yield return msg;
                if (msg.Payload.Choices.Status == 2)
                {
                    await CloseAsync();
                    break;
                }
            }
        }
    }
}

internal class XFSparkDeskImageAPIRequest
{
    public double Temperature { get; set; } = 0.5;

    public int MaxTokens { get; set; }

    public int TopK { get; set; } = 4;

    public string Auditing { get; set; } = "default";

    public string Domain { get; set; } = "image";

    public List<XFSparkDeskImageAPIMessageRequest> Messages { get; set; } = null!;
}

internal class XFSparkDeskImageAPIMessageRequest
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}

internal class XFSparkDeskImageAPIResponse
{
    [JsonPropertyName("header")]
    public XFSparkDeskImageAPIHeaderResponse? Header { get; set; }

    [JsonPropertyName("payload")]
    public XFSparkDeskImageAPIPayloadResponse? Payload { get; set; }
}

internal class XFSparkDeskImageAPIHeaderResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("sid")]
    public string Sid { get; set; } = null!;

    [JsonPropertyName("status")]
    public int Status { get; set; }
}

internal class XFSparkDeskImageAPIPayloadResponse
{
    [JsonPropertyName("choices")]
    public XFSparkDeskImageAPIPayloadChoicesResponse Choices { get; set; } = null!;

    [JsonPropertyName("usage")]
    public XFSparkDeskImageAPIPayloadUsageResponse? Usage { get; set; }
}

internal class XFSparkDeskImageAPIPayloadChoicesResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("text")]
    public List<XFSparkDeskImageAPIPayloadChoicesTextResponse> Text { get; set; } = null!;
}

internal class XFSparkDeskImageAPIPayloadChoicesTextResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("content_meta")]
    public XFSparkDeskImageAPIPayloadChoicesTextContentMetaResponse? ContentMeta { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("index")]
    public int Index { get; set; }
}

internal class XFSparkDeskImageAPIPayloadChoicesTextContentMetaResponse
{
    [JsonPropertyName("content")]
    public string? Desc { get; set; }

    [JsonPropertyName("role")]
    public bool Url { get; set; }
}

internal class XFSparkDeskImageAPIPayloadUsageResponse
{
    [JsonPropertyName("text")]
    public XFSparkDeskImageAPIPayloadUsageTextResponse Text { get; set; } = null!;
}

internal class XFSparkDeskImageAPIPayloadUsageTextResponse
{
    [JsonPropertyName("question_tokens")]
    public int QuestionTokens { get; set; }

    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}