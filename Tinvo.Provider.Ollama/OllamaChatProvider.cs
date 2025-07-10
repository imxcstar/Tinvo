using Ollama;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;

namespace Tinvo.Provider.Ollama
{
    [TypeMetadataDisplayName("聊天配置")]
    public class OllamaChatConfig
    {
        public string Url { get; set; } = "";
        public string Model { get; set; } = "";
        [TypeMetadataAllowNull]
        public string? ReasoningStartToken { get; set; }
        [TypeMetadataAllowNull]
        public string? ReasoningEndToken { get; set; }
    }

    [ProviderTask("OllamaChat", "Ollama")]
    public class OllamaChatProvider : IAIChatTask
    {
        private OllamaChatConfig _config;
        private string _url;
        private string _model;
        private OllamaApiClient _client;

        public Task InitAsync()
        {
            return Task.CompletedTask;
        }

        public OllamaChatProvider(OllamaChatConfig config)
        {
            _config = config;
            _url = config.Url;
            _model = config.Model;
            if (!_url.EndsWith("/api"))
                _url = $"{_url.TrimEnd('/')}/api";
            _client = new OllamaApiClient(baseUri: new Uri(_url));
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> ChatAsync(ChatHistory chat, ChatSettings? chatSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                        if (fContnet is AIProviderHandleTextMessageResponse textMessage)
                            return new Message()
                            {
                                Role = MessageRole.Assistant,
                                Content = textMessage.Message
                            };
                        else
                            throw new NotSupportedException("Ollama其它角色发送不支持的内容类型");
                    }
                    else
                    {
                        var role = x.Role switch
                        {
                            AuthorRole.User => MessageRole.User,
                            _ => MessageRole.System
                        };

                        if (fContnet is AIProviderHandleTextMessageResponse textMessage)
                            return new Message()
                            {
                                Role = role,
                                Content = textMessage.Message
                            };
                        else
                            throw new NotSupportedException("Ollama发送不支持的内容类型");
                    }
                }).Where(x => x != null).ToList()!
            };
            var ret = _client.Chat.GenerateChatCompletionAsync(request, cancellationToken);
            var isReasoning = false;
            await foreach (var item in ret)
            {
                if (string.IsNullOrEmpty(item?.Message?.Content))
                    continue;
                var content = item.Message.Content;
                if (!string.IsNullOrEmpty(_config.ReasoningStartToken) && !string.IsNullOrEmpty(_config.ReasoningEndToken))
                {
                    if (content == _config.ReasoningStartToken)
                    {
                        isReasoning = true;
                        continue;
                    }
                    if (content == _config.ReasoningEndToken)
                    {
                        isReasoning = false;
                        continue;
                    }
                }
                if (isReasoning)
                    yield return new AIProviderHandleReasoningMessageResponse()
                    {
                        Message = item.Message.Content
                    };
                else
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
                ret.AddMessage(AuthorRole.User, [new AIProviderHandleTextMessageResponse() { Message = $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")}" }]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.User, [new AIProviderHandleTextMessageResponse() { Message = instructions }]);
            return ret;
        }
    }
}
