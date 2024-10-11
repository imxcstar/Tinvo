using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;
using TrustInnova.Abstractions.AIScheduler;
using TrustInnova.Provider.XunFei.API;

namespace TrustInnova.Provider.XunFei.AIScheduler
{
    [ProviderTask("XFSparkDeskAIChatHybrid", "讯飞星火(多模态)")]
    public class XFSparkDeskAIHybridProvider : IAIChatTask
    {
        private readonly XFSparkDeskAIChatProvider _chatProvider;
        private readonly XFSparkDeskImageAnalysisAPI? _imageApi;
        private readonly IAIChatParser _parser;
        private readonly ILogger _logger;

        public XFSparkDeskAIHybridProvider(XFSparkDeskChatAPIConfig chat_config, XFSparkDeskImageAnalysisAPIConfig image_config)
        {
            _chatProvider = new XFSparkDeskAIChatProvider(chat_config);
            _imageApi = new XFSparkDeskImageAnalysisAPI(image_config);
            _parser = new XFSparkDeskProviderParser();
            _logger = Log.ForContext<XFSparkDeskAIHybridProvider>();
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-dd")}", ChatMessageContentType.Text)]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.System, [new(Guid.NewGuid().ToString(), instructions, ChatMessageContentType.Text)]);
            return ret;
        }

        public async IAsyncEnumerable<IAIChatHandleResponse> ChatAsync(ChatHistory chat, ChatSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (chat.Any(x => x.Role == AuthorRole.User && x.Contents.Any(x2 => x2.ContentType == ChatMessageContentType.ImageBase64)) && _imageApi != null)
            {
                XFSparkDeskImageAPIMessageRequest lastImage = default!;
                var imageChatMessage = chat.SelectMany(x =>
                {
                    var ret = new List<XFSparkDeskImageAPIMessageRequest>();
                    var fContnet = x.Contents.FirstOrDefault();
                    if (fContnet == null)
                        return ret;

                    if (x.Role == AuthorRole.Assistant)
                    {
                        if (fContnet.ContentType != ChatMessageContentType.Text)
                            throw new NotSupportedException("XFSpark其它角色发送不支持的内容类型");
                        ret.Add(new XFSparkDeskImageAPIMessageRequest()
                        {
                            Role = "assistant",
                            Content = (string)fContnet.Content
                        });
                    }
                    else
                    {
                        foreach (var content in x.Contents)
                        {
                            switch (content.ContentType)
                            {
                                case ChatMessageContentType.Text:
                                    ret.Add(new XFSparkDeskImageAPIMessageRequest()
                                    {
                                        Role = "user",
                                        Content = (string)content.Content,
                                        ContentType = "text"
                                    });
                                    break;
                                case ChatMessageContentType.ImageBase64:
                                    lastImage = new XFSparkDeskImageAPIMessageRequest()
                                    {
                                        Role = "user",
                                        Content = (string)content.Content,
                                        ContentType = "image"
                                    };
                                    break;
                                case ChatMessageContentType.ImageURL:
                                case ChatMessageContentType.DocStream:
                                case ChatMessageContentType.DocURL:
                                default:
                                    throw new NotSupportedException("XFSpark发送不支持的内容类型");
                            }
                        }
                    }
                    return ret;
                }).ToList();
                imageChatMessage.Insert(0, lastImage);
                var imageRet = _imageApi.SendImageChat(new XFSparkDeskImageAPIRequest()
                {
                    MaxTokens = requestSettings?.MaxTokens ?? 1024,
                    Messages = imageChatMessage
                }, cancellationToken);
                await foreach (var item in imageRet)
                {
                    var handleRet = _parser.Handle(item, requestSettings?.FunctionManager);
                    await foreach (var item2 in handleRet)
                    {
                        yield return item2;
                    }
                }
                yield break;
            }

            var fm = new FunctionManager();
            fm.AddCustomFunction("Answer", "回答用户问题", new FunctionParametersInfo()
            {
                Type = "object",
                Properties = new Dictionary<string, FunctionParametersProperties>()
                {
                    {
                        "type",
                        new FunctionParametersProperties()
                        {
                            Description="类型分别为：画图、闲聊、图片识别、未知",
                            Type="string"
                        }
                    }
                },
                Required = ["type"]
            });

            var lchat = chat.Last();
            var nm = new ChatMessage(AuthorRole.User, [new ChatMessageContent(Guid.NewGuid().ToString(), $"题目：小明说到“{(string)lchat.Contents.First().Content}”\r\n问：请回答小明想要做什么？", ChatMessageContentType.Text)]);
            var intentionRet = _chatProvider.ChatAsync([nm], new ChatSettings()
            {
                FunctionManager = fm,
                Temperature = 0.5,
                TopP = 4,
                MaxTokens = 1024
            }, cancellationToken);
            await foreach (var intention in intentionRet)
            {
                switch (intention.Type)
                {
                    case AIChatHandleResponseType.FunctionCall:
                        _logger.Debug("Intention Select(FunctionCall): ", intention.Type);
                        if (intention is not AIProviderHandleFunctionCallResponse funHandleResponse)
                        {
                            _logger.Debug("Intention Select(FunctionCall): AI执行返回解释错误：{ret}", intention);
                            break;
                        }
                        _logger.Debug("Intention Select(FunctionCall): AI开始执行：{name}", funHandleResponse.FunctionName);
                        _logger.Debug("Intention Select(FunctionCall): AI开始执行参数：{args}", funHandleResponse.Arguments);
                        break;
                    default:
                        _logger.Debug("Intention Select: {type}", intention.Type);
                        break;
                }
            }

            var chatRet = _chatProvider.ChatAsync(chat, requestSettings, cancellationToken);
            await foreach (var item in chatRet)
            {
                yield return item;
            }
        }
    }
}
