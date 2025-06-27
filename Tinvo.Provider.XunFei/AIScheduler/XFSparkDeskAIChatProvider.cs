using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;
using Tinvo.Abstractions.AIScheduler;
using Tinvo.Provider.XunFei.API;

namespace Tinvo.Provider.XunFei.AIScheduler
{
    [ProviderTask("XFSparkDeskAIChat", "讯飞星火")]
    public class XFSparkDeskAIChatProvider : IAIChatTask
    {
        private readonly XFSparkDeskChatAPI _chatApi;
        private readonly IAIChatParser _parser;
        private readonly ILogger _logger;

        public XFSparkDeskAIChatProvider(XFSparkDeskChatAPIConfig config)
        {
            _chatApi = new XFSparkDeskChatAPI(config);
            _parser = new XFSparkDeskProviderParser();
            _logger = Log.ForContext<XFSparkDeskAIChatProvider>();
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
            var ret = new ChatHistory();
            if (instructions == null)
                ret.AddMessage(AuthorRole.System, [new AIProviderHandleTextMessageResponse() { Message = $@"现在的时间为：{DateTime.Now.ToString("yyyy-MM-dd")}" }]);
            else if (!string.IsNullOrWhiteSpace(instructions))
                ret.AddMessage(AuthorRole.System, [new AIProviderHandleTextMessageResponse() { Message = instructions }]);
            return ret;
        }

        public async IAsyncEnumerable<IAIChatHandleMessage> ChatAsync(ChatHistory chat, ChatSettings? requestSettings = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var chatRet = _chatApi.SendChat(new XFSparkDeskChatAPIRequest()
            {
                MaxTokens = requestSettings?.MaxOutputTokens ?? 1024,
                Messages = chat.Select(x =>
                {
                    var fContnet = x.Contents.FirstOrDefault();
                    if (fContnet == null)
                        return null;

                    if (x.Role == AuthorRole.Assistant)
                    {
                        if (fContnet is AIProviderHandleTextMessageResponse textMessage)
                            return new XFSparkDeskChatAPIMessageRequest()
                            {
                                Role = "assistant",
                                Content = textMessage.Message
                            };
                        else
                            throw new NotSupportedException("XFSpark其它角色发送不支持的内容类型");
                    }
                    else
                    {
                        if (fContnet is AIProviderHandleTextMessageResponse textMessage)
                            return new XFSparkDeskChatAPIMessageRequest()
                            {
                                Role = "user",
                                Content = textMessage.Message
                            };
                        else
                            throw new NotSupportedException("XFSpark发送不支持的内容类型");
                    }
                }).Where(x => x != null).ToList()!,
                Functions = (requestSettings?.FunctionManager == null || requestSettings.FunctionManager.GetFunctionInfos().Count <= 0) ? null : requestSettings?.FunctionManager.GetFunctionInfos().Select(x => new XFSparkDeskChatAPIFunctionRequest()
                {
                    Name = x.Name,
                    Description = x.Description ?? "",
                    Parameters = new XFSparkDeskChatAPIFunctionParametersRequest()
                    {
                        Type = x.Type,
                        Properties = x.Parameters.Properties.ToDictionary(x2 => x2.Key, x2 => new XFSparkDeskChatAPIFunctionParametersPropertieRequest()
                        {
                            Type = x2.Value.Type,
                            Description = x2.Value.Description
                        })
                    },
                    Required = x.Parameters.Required
                }).ToList()
            }, cancellationToken);
            await foreach (var item in chatRet)
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
