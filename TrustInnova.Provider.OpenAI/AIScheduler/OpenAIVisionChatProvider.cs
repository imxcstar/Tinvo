using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Abstractions.ImageAnalysis;
using TrustInnova.Provider.OpenAI.API;

namespace TrustInnova.Provider.OpenAI.AIScheduler
{
    [ProviderTask("OpenAIChatVision", "OpenAI(视觉)")]
    public class OpenAIVisionChatProvider : IAIChatTask, IImageAnalysisTask
    {
        private readonly OpenAIChatConfig _config;
        private readonly OpenAIChatCompletionAPI _chatApi;
        private readonly IAIChatParser _parser;

        public OpenAIVisionChatProvider(OpenAIChatConfig config)
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
                                return new OpenAIChatMessage(roleName, new List<object>()
                                {
                                    new
                                    {
                                        type = "text",
                                        text = $"![图片]({fContnet.ContentId}.png)"
                                    },
                                    new
                                    {
                                        type = "image_url",
                                        image_url = new
                                        {
                                            url = $"data:image/jpeg;base64,{(string)fContnet.Content}",
                                            detail = "low"
                                        }
                                    }
                                });
                            case ChatMessageContentType.ImageURL:
                                return new OpenAIChatMessage(roleName, new List<object>()
                                {
                                    new
                                    {
                                        type = "text",
                                        text = $"![图片]({fContnet.ContentId}.png)"
                                    },
                                    new
                                    {
                                        type = "image_url",
                                        image_url = new
                                        {
                                            url = (string)fContnet.Content,
                                            detail = "low"
                                        }
                                    }
                                });
                            case ChatMessageContentType.DocStream:
                            case ChatMessageContentType.DocURL:
                            default:
                                throw new NotSupportedException("OpenAI其它角色发送不支持的内容类型");
                        }
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
                                retContent.AddRange([
                                    new
                                    {
                                        type = "text",
                                        text = $"![图片]({content.ContentId}.png)"
                                    },
                                    new
                                    {
                                        type = "image_url",
                                        image_url = new
                                        {
                                            url = $"data:image/jpeg;base64,{(string)content.Content}",
                                            detail = "low"
                                        }
                                    }
                                ]);
                                break;
                            case ChatMessageContentType.ImageURL:
                                retContent.AddRange([
                                    new
                                    {
                                        type = "text",
                                        text = $"![图片]({content.ContentId}.png)"
                                    },
                                    new
                                    {
                                        type = "image_url",
                                        image_url = new
                                        {
                                            url = (string)content.Content,
                                            detail = "low"
                                        }
                                    }
                                ]);
                                break;
                            case ChatMessageContentType.DocStream:
                            case ChatMessageContentType.DocURL:
                            default:
                                throw new NotSupportedException("OpenAI发送不支持的内容类型");
                        }
                    }
                    return ret;
                }).Where(x => x != null).ToList()!,
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

        public async IAsyncEnumerable<string> AnalysisAsync(ImageAnalysisOptions options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messageContents = new List<ChatMessageContent>()
            {
                new ChatMessageContent(Guid.NewGuid().ToString(),Convert.ToBase64String(((MemoryStream)options.Image).ToArray()),ChatMessageContentType.ImageBase64)
            };
            if (!string.IsNullOrWhiteSpace(options.Prompt))
                messageContents.Insert(0, new ChatMessageContent(Guid.NewGuid().ToString(), options.Prompt, ChatMessageContentType.Text));
            var ret = ChatAsync(new ChatHistory()
            {
                new ChatMessage(AuthorRole.User, messageContents)
            }, cancellationToken: cancellationToken);
            await foreach (var item in ret)
            {
                if (item.Type != AIChatHandleResponseType.TextMessage)
                    continue;
                var text = item as AIProviderHandleTextMessageResponse;
                if (text == null)
                    continue;
                yield return text.Message;
            }
        }
    }
}
