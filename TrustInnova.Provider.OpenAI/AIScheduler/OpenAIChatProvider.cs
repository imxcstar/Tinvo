using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.OpenAI.API;
using TrustInnova.Abstractions;
using System.ComponentModel;

namespace TrustInnova.Provider.OpenAI.AIScheduler
{
    [TypeMetadataDisplayName("聊天配置")]
    public class OpenAIChatConfig
    {
        [Description("地址")]
        [DefaultValue("https://api.openai.com")]
        public string BaseURL { get; set; } = "https://api.openai.com";

        [Description("代理(可选)")]
        [TypeMetadataAllowNull]
        public string? Proxy { get; set; }

        [Description("密钥")]
        [TypeMetadataAllowNull]
        public string? Token { get; set; }

        [Description("模型")]
        public string Model { get; set; } = null!;
    }

    [ProviderTask("OpenAIChat", "OpenAI")]
    public class OpenAIChatProvider : IAIChatTask
    {
        private readonly OpenAIChatCompletionAPI _chatApi;
        private readonly OpenAIChatConfig _config;
        private readonly IAIChatParser _parser;

        public OpenAIChatProvider(OpenAIChatConfig config)
        {
            _config = config;
            _chatApi = new OpenAIChatCompletionAPI(APIUtil.GetAPI<IOpenAIChatAPI>(_config.BaseURL, _config.Token, _config.Proxy));
            _parser = new OpenAIProviderParser();
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
                FrequencyPenalty = (float?)requestSettings?.FrequencyPenalty,
                MaxTokens = requestSettings?.MaxTokens,
                PresencePenalty = (float?)requestSettings?.PresencePenalty,
                Stream = true,
                Temperature = (float?)requestSettings?.Temperature,
                TopP = (float?)requestSettings?.TopP,
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
