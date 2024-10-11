using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;

namespace TrustInnova.Provider.Onnx
{
    [TypeMetadataDisplayName("聊天配置")]
    public class OnnxChatConfig
    {
        public string Url { get; set; } = "";
        public string Model { get; set; } = "";
    }

    [ProviderTask("OnnxChat", "Onnx")]
    public class OnnxChatProvider : IAIChatTask
    {
        private string _url;
        private string _model;
        private OllamaApiClient _client;

        public OnnxChatProvider(OllamaChatConfig config)
        {
            _url = config.Url;
            _model = config.Model;
            if (!_url.EndsWith("/api"))
                _url = $"{_url.TrimEnd('/')}/api";
            _client = new OllamaApiClient(baseUri: new Uri(_url));
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = new GenerateChatCompletionRequest()
            {
                Model = _model,
                Options = new RequestOptions()
                {
                    Temperature = (float?)chatSettings?.Temperature,
                    TopP = (float?)chatSettings?.TopP,
                    Stop = chatSettings?.StopSequences.Select(x => (string?)x).ToList(),
                },
                Messages = chat.Select(x =>
                {
                    var fContnet = x.Contents.FirstOrDefault();
                    if (fContnet == null)
                        return null;

                    if (x.Role == AuthorRole.Assistant)
                    {
                        if (fContnet.ContentType != ChatMessageContentType.Text)
                            throw new NotSupportedException("Ollama其它角色发送不支持的内容类型");
                        return new Message()
                        {
                            Role = MessageRole.Assistant,
                            Content = (string)fContnet.Content
                        };
                    }
                    else
                    {
                        var role = x.Role switch
                        {
                            AuthorRole.User => MessageRole.User,
                            _ => MessageRole.System
                        };
                        if (x.Contents.Count >= 2 && x.Contents.Any(x => x.ContentType == ChatMessageContentType.ImageBase64))
                        {
                            return new Message()
                            {
                                Role = role,
                                Content = (string?)x.Contents.FirstOrDefault(x => x.ContentType == ChatMessageContentType.Text)?.Content ?? "解释下这图片内容",
                                Images = [(string)x.Contents.First(x => x.ContentType == ChatMessageContentType.ImageBase64).Content]
                            };
                        }
                        switch (fContnet.ContentType)
                        {
                            case ChatMessageContentType.Text:
                                return new Message()
                                {
                                    Role = role,
                                    Content = (string)fContnet.Content,
                                };
                            case ChatMessageContentType.ImageBase64:
                            case ChatMessageContentType.ImageURL:
                            case ChatMessageContentType.DocStream:
                            case ChatMessageContentType.DocURL:
                            default:
                                throw new NotSupportedException("Ollama发送不支持的内容类型");
                        }
                    }
                }).Where(x => x != null).ToList()!
            };
            var ret = _client.Chat.GenerateChatCompletionAsync(request, cancellationToken);
            await foreach (var item in ret)
            {
                if (string.IsNullOrEmpty(item?.Message?.Content))
                    continue;
                yield return new AIProviderHandleTextMessageResponse()
                {
                    Message = item.Message.Content
                };
            }
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.User, [new(Guid.NewGuid().ToString(), $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}", ChatMessageContentType.Text)]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.User, [new(Guid.NewGuid().ToString(), instructions, ChatMessageContentType.Text)]);
            return ret;
        }
    }
}
