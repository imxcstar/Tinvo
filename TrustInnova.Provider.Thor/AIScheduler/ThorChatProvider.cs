using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.Thor.API;
using TrustInnova.Abstractions;
using System.ComponentModel;

namespace TrustInnova.Provider.Thor.AIScheduler
{
    [TypeMetadataDisplayName("聊天配置")]
    public class ThorChatConfig
    {
        [Description("地址")]
        [DefaultValue("https://api.token-ai.cn")]
        public string BaseURL { get; set; } = "https://api.token-ai.cn";

        [Description("代理(可选)")]
        [TypeMetadataAllowNull]
        public string? Proxy { get; set; }

        [Description("密钥")]
        [TypeMetadataAllowNull]
        public string? Auth { get; set; }

        [Description("模型")]
        public string Model { get; set; } = null!;

        [Description("MaxTokens")]
        [DefaultValue(4096)]
        public int MaxTokens { get; set; } = 4096;

        [Description("FrequencyPenalty")]
        [DefaultValue(0)]
        public float FrequencyPenalty { get; set; } = 0;

        [Description("PresencePenalty")]
        [DefaultValue(0)]
        public float PresencePenalty { get; set; } = 0;

        [Description("Temperature")]
        [DefaultValue(0.6f)]
        public float Temperature { get; set; } = 0.6f;

        [Description("TopP")]
        [DefaultValue(1)]
        public float TopP { get; set; } = 1;
    }

    [ProviderTask("ThorChat", "Thor")]
    public class ThorChatProvider : IAIChatTask
    {
        private readonly ThorChatCompletionAPI _chatApi;
        private readonly ThorChatConfig _config;
        private readonly IAIChatParser _parser;

        public ThorChatProvider(ThorChatConfig config)
        {
            _config = config;
            _chatApi = new ThorChatCompletionAPI(APIUtil.GetAPI<IThorChatAPI>(_config.BaseURL, _config.Auth, _config.Proxy));
            _parser = new ThorProviderParser();
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), $@"You are ChatGPT, a large language model trained by OpenAI.
Current date: {DateTime.Now.ToString("yyyy-MM-dd")}", ChatMessageContentType.Text)]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), instructions, ChatMessageContentType.Text)]);
            return ret;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var ret = _chatApi.SendChat(new OpenAIChatCompletionCreateRequest()
            {
                Messages = chat.Select(x =>
                {
                    var fContnet = x.Contents.FirstOrDefault();
                    if (fContnet == null)
                        return null;

                    var roleName = x.Role.ToString().ToLower();
                    if (x.Role != AuthorRole.User)
                    {
                        switch (fContnet.ContentType)
                        {
                            case ChatMessageContentType.Text:
                                return new OpenAIChatMessage(roleName, (string)fContnet.Content);
                            case ChatMessageContentType.ImageBase64:
                            case ChatMessageContentType.ImageURL:
                                return new OpenAIChatMessage(roleName, $"![图片]({fContnet.ContentId}.png)");
                            case ChatMessageContentType.DocStream:
                            case ChatMessageContentType.DocURL:
                                return new OpenAIChatMessage(roleName, $"[文档]({fContnet.ContentId}{Path.GetExtension(fContnet.ContentName)})");
                            default:
                                throw new NotSupportedException("OpenAI其它角色发送不支持的内容类型");
                        }
                    }

                    if (x.Contents.Count == 1 && x.Contents.First().ContentType == ChatMessageContentType.Text)
                    {
                        return new OpenAIChatMessage(roleName, (string)x.Contents.First().Content);
                    }

                    var retContent = new List<object>();
                    var ret = new OpenAIChatMessage(roleName, retContent);
                    foreach (var content in x.Contents)
                    {
                        switch (content.ContentType)
                        {
                            case ChatMessageContentType.Text:
                                retContent.Add(new
                                {
                                    type = "text",
                                    text = (string)content.Content
                                });
                                break;
                            case ChatMessageContentType.ImageBase64:
                            case ChatMessageContentType.ImageURL:
                                retContent.Add(new
                                {
                                    type = "text",
                                    text = $"![图片]({content.ContentId}.png)"
                                });
                                break;
                            case ChatMessageContentType.DocStream:
                            case ChatMessageContentType.DocURL:
                                retContent.Add(new
                                {
                                    type = "text",
                                    text = $"[文档]({content.ContentId}{Path.GetExtension(content.ContentName)})"
                                });
                                break;
                            default:
                                throw new NotSupportedException("OpenAI发送不支持的内容类型");
                        }
                    }
                    return ret;
                }).Where(x => x != null).ToList()!,
                ToolChoice = (requestSettings?.FunctionManager == null || requestSettings.FunctionManager.FunctionInfos.Count <= 0) ? null : "auto",
                StopAsList = requestSettings == null ? null : requestSettings.StopSequences.Any() ? requestSettings.StopSequences : null,
                FrequencyPenalty = (float?)requestSettings?.FrequencyPenalty ?? _config.FrequencyPenalty,
                MaxTokens = requestSettings?.MaxTokens ?? _config.MaxTokens,
                PresencePenalty = (float?)requestSettings?.PresencePenalty ?? _config.PresencePenalty,
                Stream = true,
                Temperature = (float?)requestSettings?.Temperature ?? _config.Temperature,
                TopP = (float?)requestSettings?.TopP ?? _config.TopP,
                Model = _config.Model,
                Functions = (requestSettings?.FunctionManager == null || requestSettings.FunctionManager.FunctionInfos.Count <= 0) ? null : requestSettings.FunctionManager.FunctionInfos.Adapt<List<OpenAIFunctionInfo>>()
            }, cancellationToken);
            await foreach (var item in ret)
            {
                var handleRet = _parser.Handle(item, requestSettings?.FunctionManager);
                await foreach (var item2 in handleRet)
                {
                    yield return item2;
                }
            }
        }
    }
}
