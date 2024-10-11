using Serilog;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrustInnova.Abstractions;
namespace TrustInnova.Provider.XunFei.API;

[TypeMetadataDisplayName("聊天配置")]
public class XFSparkDeskChatAPIConfig
{
    [Description("地址")]
    public string HostURL { get; set; } = null!;

    [Description("模型类型")]
    public XFSparkDeskModelType ModeType { get; set; }

    public string AppId { get; set; } = null!;
    public string ApiKey { get; set; } = null!;
    public string ApiSecret { get; set; } = null!;
}

public enum XFSparkDeskModelType
{
    V1_5,
    V2_0,
    V3_0,
    V3_5
}

internal class XFSparkDeskChatAPI
{
    private readonly XFSparkDeskChatAPIConfig _config;
    private ClientWebSocket _client { get; set; }
    private readonly ILogger _logger;

    private byte[] _buffer = new byte[1024 * 1024 * 10];

    public XFSparkDeskChatAPI(XFSparkDeskChatAPIConfig config)
    {
        NewClient();
        _config = config;
        _logger = Log.ForContext<XFSparkDeskChatAPI>();
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

    private async Task Send(XFSparkDeskChatAPIRequest request, CancellationToken cancellationToken = default)
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
                    domain = _config.ModeType switch
                    {
                        XFSparkDeskModelType.V1_5 => "general",
                        XFSparkDeskModelType.V2_0 => "generalv2",
                        XFSparkDeskModelType.V3_0 => "generalv3",
                        XFSparkDeskModelType.V3_5 => "generalv3.5",
                        _ => throw new Exception("不支持的讯飞模型")
                    },
                    temperature = request.Temperature,
                    max_tokens = request.MaxTokens,
                    top_k = request.TopK
                },
            },
            payload = new
            {
                message = new
                {
                    text = request.Messages.Select(x => new
                    {
                        role = x.Role,
                        content = x.Content
                    }).ToList()
                },
                functions = request.Functions == null ? null : new
                {
                    text = request.Functions.Select(x => new
                    {
                        name = x.Name,
                        description = x.Description,
                        parameters = new
                        {
                            type = x.Parameters.Type,
                            properties = x.Parameters.Properties.ToDictionary(x2 => x2.Key, x2 => new
                            {
                                type = x2.Value.Type,
                                description = x2.Value.Description
                            }),
                            required = x.Required
                        }
                    }).ToList()
                }
            }
        };
        var sendStr = JsonSerializer.Serialize(data, XFUtils.JsonSerializerOptions);
        _logger.Debug("XFSparkDeskChatAPI Send(RAW): {value}", sendStr);
        await _client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(sendStr)), WebSocketMessageType.Text, true, cancellationToken);
    }

    public async IAsyncEnumerable<XFSparkDeskChatAPIResponse> SendChat(XFSparkDeskChatAPIRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
            _logger.Debug($"XFSparkDeskChat ReceiveMessages: {DateTime.Now}");
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await CloseAsync();
                break;
            }
            else
            {
                var message = Encoding.UTF8.GetString(_buffer, 0, result.Count);
                _logger.Debug($"XFSparkDeskChat RawMessage: {message}");
                var msg = JsonSerializer.Deserialize<XFSparkDeskChatAPIResponse>(message);
                if (msg == null || msg.Header == null || msg.Payload == null)
                {
                    _logger.Error($"XFSparkDeskChat RawMessage Deserialize Error: {message}");
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

internal class XFSparkDeskChatAPIRequest
{
    public double Temperature { get; set; } = 0.5;

    public int MaxTokens { get; set; }

    public int TopK { get; set; } = 4;

    public List<XFSparkDeskChatAPIMessageRequest> Messages { get; set; } = null!;

    public List<XFSparkDeskChatAPIFunctionRequest>? Functions { get; set; }
}

internal class XFSparkDeskChatAPIMessageRequest
{
    public string Role { get; set; } = null!;
    public string Content { get; set; } = null!;
}

internal class XFSparkDeskChatAPIFunctionRequest
{
    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public XFSparkDeskChatAPIFunctionParametersRequest Parameters { get; set; } = null!;

    public List<string> Required { get; set; } = [];
}

internal class XFSparkDeskChatAPIFunctionParametersRequest
{
    public string Type { get; set; } = null!;
    public Dictionary<string, XFSparkDeskChatAPIFunctionParametersPropertieRequest> Properties { get; set; } = [];
}

internal class XFSparkDeskChatAPIFunctionParametersPropertieRequest
{
    public string Type { get; set; } = null!;

    public string Description { get; set; } = null!;
}

internal class XFSparkDeskChatAPIResponse
{
    [JsonPropertyName("header")]
    public XFSparkDeskChatAPIHeaderResponse? Header { get; set; }

    [JsonPropertyName("payload")]
    public XFSparkDeskChatAPIPayloadResponse? Payload { get; set; }
}

internal class XFSparkDeskChatAPIHeaderResponse
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

internal class XFSparkDeskChatAPIPayloadResponse
{
    [JsonPropertyName("choices")]
    public XFSparkDeskChatAPIPayloadChoicesResponse Choices { get; set; } = null!;

    [JsonPropertyName("usage")]
    public XFSparkDeskChatAPIPayloadUsageResponse? Usage { get; set; }
}

internal class XFSparkDeskChatAPIPayloadChoicesResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("seq")]
    public int Seq { get; set; }

    [JsonPropertyName("text")]
    public List<XFSparkDeskChatAPIPayloadChoicesTextResponse> Text { get; set; } = null!;
}

internal class XFSparkDeskChatAPIPayloadChoicesTextResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("function_call")]
    public XFSparkDeskChatAPIPayloadChoicesTextFunctionCallResponse? FunctionCall { get; set; }
}

internal class XFSparkDeskChatAPIPayloadChoicesTextFunctionCallResponse
{
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}

internal class XFSparkDeskChatAPIPayloadUsageResponse
{
    [JsonPropertyName("text")]
    public XFSparkDeskChatAPIPayloadUsageTextResponse Text { get; set; } = null!;
}

internal class XFSparkDeskChatAPIPayloadUsageTextResponse
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